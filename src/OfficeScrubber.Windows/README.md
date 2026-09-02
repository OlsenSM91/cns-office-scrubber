# Windows detection adapters

These adapters only read process, file-system, and registry state. Registry-backed
detectors inspect both 32-bit and 64-bit views where Windows redirects keys. AppX
detection is deliberately omitted: reliable package enumeration would require an
additional Windows SDK dependency or a shell command, neither of which is suitable
for the dependency-free, read-only detector layer.
