<div align="center">

# JANUS Dock

### Una barra de accesos moderna y personalizable para Windows

**Windows 10 / 11 · x64 · C# / .NET 8 · Código abierto**

[![Descargar versión 6.3.97](https://img.shields.io/badge/Descargar-v6.3.97-2563eb?style=for-the-badge&logo=github)](https://github.com/momocrackcorp/janus-dock/releases/tag/v6.3.97)
[![Última compilación](https://github.com/momocrackcorp/janus-dock/actions/workflows/build-janus-dock.yml/badge.svg)](https://github.com/momocrackcorp/janus-dock/actions/workflows/build-janus-dock.yml)
[![Licencia](https://img.shields.io/github/license/momocrackcorp/janus-dock?style=flat-square)](LICENSE)

</div>

---

**JANUS Dock** es una barra independiente para acceder rápidamente a programas, carpetas, documentos y unidades de disco, con indicadores de rendimiento y apariencia configurable. Se ejecuta en Windows y no necesita JANUS ni JANUS Plus.

## Descargar

**Recomendado: instalador autónomo para Windows x64.** Si ya instalaste la versión anterior, cierra JANUS Dock y ejecuta el nuevo instalador: actualiza la instalación en la misma carpeta y conserva la configuración del usuario. Instala JANUS Dock en una carpeta fija, crea el acceso del menú Inicio y permite activar el arranque con Windows. También puedes usar la edición portátil.


**[Ir a la última versión pública — v6.3.97](https://github.com/momocrackcorp/janus-dock/releases/tag/v6.3.97)**

| Archivo | ¿Cuál elegir? |
| --- | --- |
| **JANUS-Dock-Setup-6.3.97-Autonomo.exe** | Instalador recomendado: incorpora .NET y permite el inicio automático con Windows. | 
| **JANUS-Dock-Autonomo-Windows-x64.zip** | Recomendado. Incluye el entorno .NET; no requiere instalación adicional de .NET. |
| **JANUS-Dock-Liviano-Windows-x64.zip** | Descarga más pequeña. Requiere **.NET 8 Desktop Runtime x64** instalado. |
| **SHA256SUMS.txt** | Permite comprobar la integridad de los ZIP. |

1. Descarga el instalador recomendado o el ZIP que prefieras desde **Assets** de la Release.
2. Con el instalador, sigue el asistente. Con un ZIP, extrae el contenido en una carpeta permanente.
3. Ejecuta JANUS Dock desde el menú Inicio o, en el modo portátil, `JanusDock.exe`.
4. Haz **clic derecho sobre el fondo del Dock** para abrir el menú de accesos y opciones.

> Windows SmartScreen puede mostrar una advertencia si el ejecutable no está firmado digitalmente. Verifica siempre que lo hayas descargado desde la página oficial del repositorio.

## Características

- **Cuatro posiciones:** arriba, abajo, izquierda o derecha.
- **Accesos personalizados:** agrega programas, carpetas, documentos y unidades; admite arrastrar archivos.
- **Accesos comunes:** localización de Microsoft Edge, Chrome, Brave, Firefox, Word, Excel, PowerPoint, Outlook y OneNote cuando estén instalados.
- **Iconos:** iconos nativos de Windows o estilo vectorial **Lucide** coloreado para funciones compatibles.
- **Organización:** ordena accesos, agrega separadores y ajusta la apariencia del Dock.
- **Animación:** efecto lupa para accesos y discos, y **Ctrl + rueda** sobre el Dock para ajustar el tamaño de iconos (16–96 px).
- **Unidades de disco:** iconos tipo Windows/Lucide con barra de ocupación, letra de unidad destacada, elección de colores y particiones; clic para abrir la unidad en el Explorador y detalles al pasar el mouse.
- **Tráfico de red:** subida sobre descarga en un bloque compacto, con **promedio móvil de los últimos 60 segundos**, actualizado cada segundo.
- **CPU y RAM:** indicadores independientes, con distribución vertical (uno sobre otro) u horizontal (lado a lado) configurable en **Opciones → Sistema** o con clic derecho sobre el indicador. Porcentajes actualizados cada segundo, RAM total dentro del icono y detalle de uso al pasar el mouse.
- **Energía:** botones de apagar y reiniciar con confirmación.
- **Idioma:** español de Chile, inglés y portugués de Brasil (traducción inicial en desarrollo).\n- **Inicio automático:** configurable desde Opciones y desde el instalador.\n- **Apariencia:** tamaño de iconos hasta 96 px, selectores modernos y aplicación de ajustes desde Opciones.\n- **Preferencias locales:** se guardan en `%APPDATA%\JANUS\Dock\config.json`.

> Algunas funciones y apariencias pueden variar según la configuración de Windows, la escala de pantalla y los programas instalados.

## Actualizaciones\n\nConsulta [Releases](https://github.com/momocrackcorp/janus-dock/releases) y selecciona versiones estables para uso habitual. Desde **Acerca de → Buscar actualizaciones en GitHub** puedes abrir la última versión estable y descargar el instalador para actualizar. La descarga e instalación automática desde la aplicación aún no está implementada.\n\n## Cómo agregar accesos

Haz clic derecho sobre un espacio libre del Dock y selecciona **Agregar**.

- **Programa o acceso directo:** elige un archivo o acceso existente.
- **Accesos comunes → Navegadores / Office:** agrega una aplicación detectada automáticamente.
- **Carpetas del sistema:** añade accesos como Este equipo, Documentos o Descargas.
- **Unidades de disco:** agrega una unidad disponible.

En **Opciones** puedes personalizar la posición, los iconos, los monitores y la disposición de la barra.

## Requisitos

- Windows **10 u 11, de 64 bits**.
- Para la edición liviana: [Microsoft .NET 8 Desktop Runtime (x64)](https://dotnet.microsoft.com/download/dotnet/8.0).
- La edición autónoma incluye el runtime necesario.
- Habitualmente no necesita permisos de administrador para ejecutarse.

> .NET 8 alcanza el fin de soporte en noviembre de 2026. Está prevista la evaluación de una migración a una versión LTS posterior.

## Desarrollo

Proyecto de escritorio escrito en **C#**, usando **Windows Forms** y **.NET 8**.

Para compilar desde el código fuente con el SDK de .NET 8 en Windows:

```powershell
dotnet restore
dotnet build JanusDock.csproj -c Release
```

Las compilaciones automatizadas pueden consultarse en [GitHub Actions](https://github.com/momocrackcorp/janus-dock/actions).

## Iconos de las acciones del sistema

Los botones Bloquear, Reiniciar y Apagar utilizan los símbolos incorporados en Windows mediante la fuente Segoe MDL2 Assets; no se distribuyen los antiguos PNG de Flaticon.

## Versiones y problemas

- [Versiones y descargas](https://github.com/momocrackcorp/janus-dock/releases)
- [Código fuente](https://github.com/momocrackcorp/janus-dock)
- [Reportar un problema o sugerir una mejora](https://github.com/momocrackcorp/janus-dock/issues)

JANUS Dock se distribuye como proyecto independiente del migrador JANUS.

---

<div align="center">

**Desarrollado por Momocrackcorp · Chile 🇨🇱**

</div>
