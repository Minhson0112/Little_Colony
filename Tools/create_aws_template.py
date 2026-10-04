"""Generate the reviewable CloudFormation template for the Little Colony AWS deployment."""
import json
from pathlib import Path


def ref(name):
    """Reference a CloudFormation parameter or resource."""
    return {"Ref": name}


def attr(name, attribute):
    """Read a provisioned resource attribute."""
    return {"Fn::GetAtt": [name, attribute]}


def sub(value):
    """Interpolate CloudFormation pseudo parameters and resource identifiers."""
    return {"Fn::Sub": value}


def resource(kind, properties, retain=False):
    """Describe a resource, optionally preserving player data across stack deletion or replacement."""
    result = {"Type": kind, "Properties": properties}
    if retain:
        result.update(DeletionPolicy="Retain", UpdateReplacePolicy="Retain")
    return result


def table(key):
    """Create a small encrypted provisioned table eligible for DynamoDB's capacity free tier."""
    return resource("AWS::DynamoDB::Table", {
        "AttributeDefinitions": [{"AttributeName": key, "AttributeType": "S"}],
        "KeySchema": [{"AttributeName": key, "KeyType": "HASH"}],
        "BillingMode": "PROVISIONED",
        "ProvisionedThroughput": {"ReadCapacityUnits": 5, "WriteCapacityUnits": 5},
        "SSESpecification": {"SSEEnabled": True},
        "Tags": [{"Key": "Application", "Value": "LittleColony"}]
    }, retain=True)


def api_behavior(path):
    """Forward uncached account traffic and cookies to the Lambda origin."""
    return {
        "PathPattern": path, "TargetOriginId": "Api", "ViewerProtocolPolicy": "https-only",
        "AllowedMethods": ["GET", "HEAD", "OPTIONS", "PUT", "POST", "PATCH", "DELETE"],
        "CachedMethods": ["GET", "HEAD"], "Compress": True,
        "CachePolicyId": "4135ea2d-6df8-44a3-9df3-4b5a84be39ad",
        "OriginRequestPolicyId": "b689b0a8-53d0-40ab-baf2-68738e2966ac"
    }


resources = {
    "GameBucket": resource("AWS::S3::Bucket", {
        "PublicAccessBlockConfiguration": {key: True for key in
            ["BlockPublicAcls", "BlockPublicPolicy", "IgnorePublicAcls", "RestrictPublicBuckets"]},
        "BucketEncryption": {"ServerSideEncryptionConfiguration": [
            {"ServerSideEncryptionByDefault": {"SSEAlgorithm": "AES256"}}]},
        "OwnershipControls": {"Rules": [{"ObjectOwnership": "BucketOwnerEnforced"}]}
    }, retain=True),
    "PlayersTable": table("identityId"),
    "SavesTable": table("playerId"),
    "ApiRole": resource("AWS::IAM::Role", {
        "AssumeRolePolicyDocument": {"Version": "2012-10-17", "Statement": [{
            "Effect": "Allow", "Principal": {"Service": "lambda.amazonaws.com"}, "Action": "sts:AssumeRole"}]},
        "Policies": [{"PolicyName": "LittleColonyRuntime", "PolicyDocument": {
            "Version": "2012-10-17", "Statement": [
                {"Effect": "Allow", "Action": ["dynamodb:GetItem", "dynamodb:PutItem"],
                    "Resource": [attr("PlayersTable", "Arn"), attr("SavesTable", "Arn")]},
                {"Effect": "Allow", "Action": "dynamodb:ListTables", "Resource": "*"},
                {"Effect": "Allow", "Action": ["logs:CreateLogStream", "logs:PutLogEvents"],
                    "Resource": sub("arn:${AWS::Partition}:logs:${AWS::Region}:${AWS::AccountId}:log-group:/aws/lambda/${AWS::StackName}-api:*")},
                {"Effect": "Allow", "Action": ["ssm:GetParametersByPath", "ssm:PutParameter"],
                    "Resource": [sub("arn:${AWS::Partition}:ssm:${AWS::Region}:${AWS::AccountId}:parameter/little-colony/${AWS::StackName}/keys"),
                        sub("arn:${AWS::Partition}:ssm:${AWS::Region}:${AWS::AccountId}:parameter/little-colony/${AWS::StackName}/keys/*")]},
                {"Effect": "Allow", "Action": "ssm:GetParametersByPath",
                    "Resource": [sub("arn:${AWS::Partition}:ssm:${AWS::Region}:${AWS::AccountId}:parameter/little-colony/${AWS::StackName}/authentication"),
                        sub("arn:${AWS::Partition}:ssm:${AWS::Region}:${AWS::AccountId}:parameter/little-colony/${AWS::StackName}/authentication/*")]},
                {"Effect": "Allow", "Action": ["kms:Encrypt", "kms:Decrypt"], "Resource": "*",
                    "Condition": {"StringEquals": {"kms:ViaService": sub("ssm.${AWS::Region}.amazonaws.com"),
                        "kms:CallerAccount": ref("AWS::AccountId")}}}
            ]}}]
    }),
    "ApiLogs": resource("AWS::Logs::LogGroup", {
        "LogGroupName": sub("/aws/lambda/${AWS::StackName}-api"), "RetentionInDays": 7
    }),
    "ApiFunction": resource("AWS::Lambda::Function", {
        "FunctionName": sub("${AWS::StackName}-api"), "Runtime": "dotnet8", "Architectures": ["arm64"],
        "Handler": "LittleColony.Api", "MemorySize": 512, "Timeout": 20,
        "Role": attr("ApiRole", "Arn"),
        "Code": {"S3Bucket": ref("ArtifactBucket"), "S3Key": ref("ApiArtifactKey")},
        "Environment": {"Variables": {
            "ASPNETCORE_ENVIRONMENT": "Production", "AllowedHosts": "*",
            "DynamoDb__Region": ref("AWS::Region"), "DynamoDb__PlayersTable": ref("PlayersTable"),
            "DynamoDb__SavesTable": ref("SavesTable"), "Hosting__PublicOrigin": ref("PublicOrigin"),
            "Hosting__OriginToken": ref("OriginToken"),
            "DataProtection__ParameterPath": sub("/little-colony/${AWS::StackName}/keys"),
            "Authentication__ParameterPath": sub("/little-colony/${AWS::StackName}/authentication")
        }}
    }),
    "ApiUrl": resource("AWS::Lambda::Url", {"TargetFunctionArn": attr("ApiFunction", "Arn"), "AuthType": "NONE"}),
    "ApiUrlPermission": resource("AWS::Lambda::Permission", {
        "FunctionName": ref("ApiFunction"), "Action": "lambda:InvokeFunctionUrl",
        "Principal": "*", "FunctionUrlAuthType": "NONE"
    }),
    "ApiInvokePermission": resource("AWS::Lambda::Permission", {
        "FunctionName": ref("ApiFunction"), "Action": "lambda:InvokeFunction",
        "Principal": "*", "InvokedViaFunctionUrl": True
    }),
    "GameOriginControl": resource("AWS::CloudFront::OriginAccessControl", {
        "OriginAccessControlConfig": {"Name": sub("${AWS::StackName}-game"),
            "OriginAccessControlOriginType": "s3", "SigningBehavior": "always", "SigningProtocol": "sigv4"}
    }),
    "GameDistribution": resource("AWS::CloudFront::Distribution", {
        "DistributionConfig": {
            "Comment": "Little Colony WebGL and authenticated cloud saves", "Enabled": True,
            "DefaultRootObject": "index.html", "HttpVersion": "http2", "IPV6Enabled": True,
            "PriceClass": "PriceClass_100", "ViewerCertificate": {"CloudFrontDefaultCertificate": True},
            "Origins": [
                {"Id": "Game", "DomainName": attr("GameBucket", "RegionalDomainName"),
                    "OriginAccessControlId": ref("GameOriginControl"), "S3OriginConfig": {"OriginAccessIdentity": ""}},
                {"Id": "Api", "DomainName": {"Fn::Select": [2, {"Fn::Split": ["/", attr("ApiUrl", "FunctionUrl")]}]},
                    "OriginCustomHeaders": [{"HeaderName": "X-LittleColony-Origin", "HeaderValue": ref("OriginToken")}],
                    "CustomOriginConfig": {"OriginProtocolPolicy": "https-only", "OriginSSLProtocols": ["TLSv1.2"]}}
            ],
            "DefaultCacheBehavior": {"TargetOriginId": "Game", "ViewerProtocolPolicy": "redirect-to-https",
                "AllowedMethods": ["GET", "HEAD", "OPTIONS"], "CachedMethods": ["GET", "HEAD"],
                "Compress": True, "CachePolicyId": "658327ea-f89d-4fab-a63d-7e88639e58f6"},
            "CacheBehaviors": [api_behavior(path) for path in ["api/*", "auth/*", "signin-*", "health/*"]]
        }
    }),
    "GameBucketPolicy": resource("AWS::S3::BucketPolicy", {
        "Bucket": ref("GameBucket"), "PolicyDocument": {"Version": "2012-10-17", "Statement": [{
            "Effect": "Allow", "Principal": {"Service": "cloudfront.amazonaws.com"}, "Action": "s3:GetObject",
            "Resource": sub("${GameBucket.Arn}/*"), "Condition": {"StringEquals": {
                "AWS:SourceArn": sub("arn:${AWS::Partition}:cloudfront::${AWS::AccountId}:distribution/${GameDistribution}")}}
        }]}
    })
}
resources["ApiFunction"]["DependsOn"] = ["ApiLogs"]
template = {
    "AWSTemplateFormatVersion": "2010-09-09",
    "Description": "Little Colony: private S3 assets, HTTPS CDN, Lambda API and retained DynamoDB saves.",
    "Parameters": {
        "ArtifactBucket": {"Type": "String"}, "ApiArtifactKey": {"Type": "String"},
        "OriginToken": {"Type": "String", "NoEcho": True, "MinLength": 32},
        "PublicOrigin": {"Type": "String", "Default": "", "Description": "CloudFront HTTPS origin; set after initial provisioning."}
    },
    "Resources": resources,
    "Outputs": {
        "WebsiteUrl": {"Value": sub("https://${GameDistribution.DomainName}")},
        "GameBucket": {"Value": ref("GameBucket")}, "DistributionId": {"Value": ref("GameDistribution")},
        "ApiFunction": {"Value": ref("ApiFunction")},
        "AuthenticationParameterPath": {"Value": sub("/little-colony/${AWS::StackName}/authentication")}
    }
}
root = Path(__file__).resolve().parents[1]
output = root / "Infrastructure/aws-template.json"
output.parent.mkdir(exist_ok=True)
output.write_text(json.dumps(template, indent=2) + "\n", encoding="utf-8")
print("Generated " + str(output))
