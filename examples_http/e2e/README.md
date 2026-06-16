# Safe E2E Scenarios

These flows cover the manual end-to-end paths we want to keep after the cleanup.

Recommended order:

1. `10_tools_generate_basic_prg.http`
2. `11_tools_generate_assembly_prg.http`
3. `12_tools_run_prg_binary.http`
4. `13_tools_play_sid_binary.http`
5. `14_tools_create_disk_images.http`

Notes:

- These scenarios are intended for `smoke + safe flows`.
- They may depend on a live Ultimate device reachable through `Ultimate__BaseUrl`.
- Asset-based tests use `./examples_http/matrix.prg` and `./examples_http/matrix.sid`, which are mounted into the container by `docker-compose.yml`.
