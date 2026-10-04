#!/usr/bin/env bash
# Deploy a reviewed bundle from AWS CloudShell; never print private parameters.
set -euo pipefail
export AWS_PAGER=""
region="${AWS_REGION:-ap-southeast-2}"
stack="little-colony-production"
account="$(aws sts get-caller-identity --query Account --output text)"
artifact_bucket="little-colony-artifacts-${account}-${region}"
if ! aws s3api head-bucket --bucket "$artifact_bucket" 2>/dev/null; then
    aws s3api create-bucket --bucket "$artifact_bucket" --region "$region" \
        --create-bucket-configuration "LocationConstraint=$region" >/dev/null
fi
aws s3api put-public-access-block --bucket "$artifact_bucket" --public-access-block-configuration \
    'BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true'
aws s3api put-bucket-encryption --bucket "$artifact_bucket" --server-side-encryption-configuration \
    '{"Rules":[{"ApplyServerSideEncryptionByDefault":{"SSEAlgorithm":"AES256"}}]}'
artifact_key="api/$(sha256sum api.zip | cut -d' ' -f1).zip"
aws s3 cp api.zip "s3://${artifact_bucket}/${artifact_key}" --only-show-errors
if [[ ! -f .deployment-parameters.json ]]; then
    python3 - "$artifact_bucket" "$artifact_key" <<'PY'
import json, os, secrets, sys
values = dict(ArtifactBucket=sys.argv[1], ApiArtifactKey=sys.argv[2], OriginToken=secrets.token_hex(32), PublicOrigin="")
with open('.deployment-parameters.json', 'w') as stream:
    json.dump([dict(ParameterKey=k, ParameterValue=v) for k,v in values.items()], stream)
os.chmod('.deployment-parameters.json', 0o600)
PY
fi
python3 - "$artifact_bucket" "$artifact_key" <<'PY'
import json, sys
params = json.load(open('.deployment-parameters.json'))
for param in params:
    if param['ParameterKey'] == 'ArtifactBucket': param['ParameterValue'] = sys.argv[1]
    if param['ParameterKey'] == 'ApiArtifactKey': param['ParameterValue'] = sys.argv[2]
with open('.deployment-parameters.json', 'w') as stream: json.dump(params, stream)
PY
aws cloudformation deploy --stack-name "$stack" --template-file aws-template.json --region "$region" \
    --capabilities CAPABILITY_IAM --parameter-overrides file://.deployment-parameters.json --no-fail-on-empty-changeset
aws cloudformation describe-stacks --stack-name "$stack" --query 'Stacks[0].Outputs' > .deployment-outputs.json
game_bucket="$(python3 -c 'import json; print(next(x["OutputValue"] for x in json.load(open(".deployment-outputs.json")) if x["OutputKey"]=="GameBucket"))')"
aws s3 sync WebGL/ "s3://${game_bucket}/" --cache-control 'public,max-age=3600' --only-show-errors
aws s3 cp WebGL/index.html "s3://${game_bucket}/index.html" --content-type text/html --cache-control no-cache --only-show-errors
aws s3 cp WebGL/i18n-web.json "s3://${game_bucket}/i18n-web.json" --content-type application/json --cache-control no-cache --only-show-errors
for extension in wasm data; do
    if [[ "$extension" == wasm ]]; then content_type=application/wasm; else content_type=application/octet-stream; fi
    for file in WebGL/Build/*.$extension; do
        aws s3 cp "$file" "s3://${game_bucket}/Build/$(basename "$file")" --content-type "$content_type" --only-show-errors
    done
done
python3 - <<'PY'
import json
outputs = {x['OutputKey']: x['OutputValue'] for x in json.load(open('.deployment-outputs.json'))}
params = json.load(open('.deployment-parameters.json'))
for param in params:
    if param['ParameterKey'] == 'PublicOrigin': param['ParameterValue'] = outputs['WebsiteUrl']
with open('.deployment-parameters.json', 'w') as stream: json.dump(params, stream)
PY
aws cloudformation deploy --stack-name "$stack" --template-file aws-template.json --region "$region" \
    --capabilities CAPABILITY_IAM --parameter-overrides file://.deployment-parameters.json --no-fail-on-empty-changeset
distribution="$(python3 -c 'import json; print(next(x["OutputValue"] for x in json.load(open(".deployment-outputs.json")) if x["OutputKey"]=="DistributionId"))')"
aws cloudfront create-invalidation --distribution-id "$distribution" --paths '/*' --query Invalidation.Id --output text
python3 -c 'import json; print(next(x["OutputValue"] for x in json.load(open(".deployment-outputs.json")) if x["OutputKey"]=="WebsiteUrl"))'
