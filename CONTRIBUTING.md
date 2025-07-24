## 🗼️ Contribuir a ShutdownApp

¡Gracias por tu interés en contribuir a ShutdownApp! A continuación, encontrarás las guías para compilar el proyecto desde el código fuente y entender el flujo de trabajo de automatización.

## 📦 Instalación y Compilación

Si deseas compilar el proyecto desde el código fuente, necesitarás:

*   **Visual Studio 2022** o superior con la carga de trabajo "Desarrollo de escritorio con .NET".
*   **.NET 6.0 SDK**.

Para compilar:

1.  Clona este repositorio:
    ```bash
    git clone https://github.com/Pablitus666/ShutdownApp.git
    ```
2.  Abre el archivo `ShutdownApp/ShutdownApp.csproj` en Visual Studio.
3.  Compila el proyecto en modo `Release` para la plataforma `win-x64`. El ejecutable resultante se encontrará en `ShutdownApp/bin/Release/net6.0-windows/win-x64/publish/ShutdownApp.exe`.

## 🚀 Automatización con GitHub Actions

Este repositorio incluye un flujo de trabajo de GitHub Actions para automatizar la compilación y publicación del ejecutable cada vez que se realizan cambios en la rama `main`.

El archivo `.github/workflows/build.yml` contiene la configuración:

```yaml
name: Build and Publish EXE

on:
  push:
    branches: [main]
  workflow_dispatch:

jobs:
  build:
    runs-on: windows-latest

    steps:
    - uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '6.0.x' # Specify the .NET version used in the project

    - name: Restore dependencies
      run: dotnet restore ShutdownApp/ShutdownApp.csproj

    - name: Build and Publish
      run: dotnet publish ShutdownApp/ShutdownApp.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:UseAppHost=true -o publish

    - name: Upload EXE
      uses: actions/upload-artifact@v4
      with:
        name: ShutdownApp
        path: publish/ShutdownApp.exe
```
