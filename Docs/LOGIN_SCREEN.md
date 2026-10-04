# Garden welcome screen — 04/10/2026

The welcome screen is rendered inside Unity by `VillageLoginScreen`, using the
same English/Vietnamese catalog as the gameplay interface. `LoginScreenStyles`
owns its generated masks and typography. `VillageGame` creates the screen after
loading the existing village and blocks gameplay input until guest entry fades
out. The simulation and existing autosave continue normally behind the screen.
The gameplay HUD is hidden while the welcome screen is visible.

## Player flow

- English is the default for a fresh installation; an existing language
  preference is respected. EN/VI changes the shared preference immediately.
- Sound can be muted or restored before entering the garden.
- Discord remains marked as coming soon. Facebook starts same-origin OAuth
  when the backend has provider credentials; otherwise it shows an availability
  message. An authenticated player can load their account village from the card.
- Play as a guest opens the existing local village after a short fade. It does
  not reset the village, modify its format, or enable cloud save.
- Landscape uses a headline beside a card; portrait stacks the headline and
  the same provider/guest controls. Buttons have pointer and keyboard focus
  feedback. All textures created at runtime are released on teardown.

`VillageCloudSave` discovers the server session, loads an account's snapshot,
preserves guest data, and synchronizes through conditional revisions. See
`FACEBOOK_CLOUD_SAVE.md` for setup and recovery behavior. Provider credentials
and a live Facebook acceptance check are still needed. Account linking
remains future work. Discord authentication is implemented;
see [DISCORD_CLOUD_SAVE.md](DISCORD_CLOUD_SAVE.md) for configuration.

## Illustration provenance

The original storybook artwork is stored at
`Assets/Resources/UI/LoginGarden.png`. It was generated with the built-in
ImageGen tool, not the CLI/API fallback. It is welcome-screen artwork, not a
Unity gameplay capture or a Blender preview. No original Bug Village art was
used as a reference. The existing Be Vietnam Pro font and its OFL license are
reused. Small interface marks are drawn in code.

Final generation prompt:

> Create one polished landscape 16:9 1536x864 raster background illustration for the welcome/sign-in screen of an original cozy insect village game called Little Colony. This is just artwork, NO text, NO typography, NO logos, NO buttons, NO UI. Hand-painted storybook gouache with soft grain, elegant restrained pastel olive and sage foliage, warm ivory sky, honey golden late afternoon light, touches of burnt terracotta and dusty coral. Composition: a charming tiny community of mushroom cottages and acorn homes nested among immense soft botanical leaves, little ants and bees, clover, small daisies, a curving narrow stream with miniature wooden bridge. Main richly detailed village scene occupies left 60% and lower portion, quiet light cream misty botanical space on right 40% for a separate login card. Keep top left 35% airy and light cream with faint leaves, suitable for dark green title overlay. Strong inviting layered depth, beautiful art direction, organic sophisticated indie game cover illustration, NOT glossy photorealistic, NOT generic 3D render, NOT copied assets from any game, no square borders or frame. Large leaves at bottom corners, floating warm light motes, clean legible silhouettes, a tranquil little garden to return to.

## Verification on 2026-10-04

- .NET Release solution build: zero warnings and errors.
- API integration executable: 41 checks passed against DynamoDB Local;
  Facebook's token/profile responses were simulated inside the test host.
- Domain/localization executable: 2,714 checks passed.
- Unity Play Mode: creation, worksite effects, account village replacement,
  60 animation frames, and teardown passed (`Logs/runtime-creation-result.txt`).
- Real WebGL browser: desktop and portrait layouts, English/Vietnamese switch,
  unavailable Facebook message, and guest entry checked without console errors.
- Local reverse proxy: session and Set-Cookie forwarding, 401 for anonymous saves,
  503 for unconfigured Facebook, and static assets checked on a temporary port.
- Live Meta sign-in and the authenticated Unity synchronization flow still need
  acceptance testing after the owner creates/configures the Facebook app.

`login-screen-webgl.png` is a real Unity WebGL screen capture. The underlying
illustration provenance and exact built-in ImageGen prompt are recorded above.
