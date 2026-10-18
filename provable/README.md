# provable

A minimal [P](https://p-org.github.io/P/) model that compiles and is ready for
model checking.

## Run it

From this directory:

```powershell
p compile
p check .\PGenerated\PChecker\net8.0\provable.dll -tc tcMain -s 100
```

Source models live in `PSrc/`; model-checking scenarios live in `PTst/`.
Generated code and checker output are intentionally ignored by Git.
