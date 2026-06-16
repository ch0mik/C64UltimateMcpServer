# Dangerous Scenarios

These requests can change persistent device state or interrupt a running session.

Files:

1. `90_machine_reset.http`
2. `91_machine_reboot_device.http`
3. `92_machine_power_off.http`
4. `93_config_save_and_reset.http`

Notes:

- Do not include these in the default smoke or safe E2E pass.
- Run them only when you intentionally want to verify those operations on a target device.
