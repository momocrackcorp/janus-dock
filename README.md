<div align="center">

# JANUS Dock

### Una barra de accesos moderna y personalizable para Windows

**Windows 10 / 11 · x64 · C# / .NET 8 · Código abierto**

[![Descargar versión 6.4.87](https://img.shields.io/badge/Descargar-v6.4.87-2563eb?style=for-the-badge&logo=github)](https://github.com/momocrackcorp/janus-dock/releases/tag/v6.4.87)
[![Compilación](https://github.com/momocrackcorp/janus-dock/actions/workflows/build-janus-dock.yml/badge.svg)](https://github.com/momocrackcorp/janus-dock/actions/workflows/build-janus-dock.yml)

</div>

---

**JANUS Dock** es una barra independiente de accesos rápidos para Windows 10 y 11. Permite abrir programas, carpetas y unidades de disco, controlar funciones del sistema y consultar indicadores de rendimiento sin depender de JANUS ni JANUS Plus.

## Descargar JANUS Dock 6.4.87

**[Ir a la versión pública 6.4.87](https://github.com/momocrackcorp/janus-dock/releases/tag/v6.4.87)**

| Archivo | Uso |
| --- | --- |
| **JANUS-Dock-Setup-6.4.87-Autonomo.exe** | **Recomendado.** Instalador para Windows x64 con .NET incluido. Actualiza la instalación anterior. |
| **JANUS-Dock-Autonomo-Windows-x64.zip** | Versión portátil, incluye .NET. |
| **JANUS-Dock-Liviano-Windows-x64.zip** | Versión portátil; necesita **.NET 8 Desktop Runtime x64**. |
| **SHA256SUMS.txt** | Sumas SHA-256 de los tres archivos de descarga. |

Para actualizar desde 6.3.97 o una versión anterior, **cierra JANUS Dock y ejecuta el instalador 6.4.87**. Se conservan la ubicación de instalación y los ajustes personales de `%APPDATA%\JANUS\Dock\config.json`; no es necesario desinstalar la versión previa. En una instalación nueva, el asistente permite crear acceso directo y configurar el inicio con Windows. Con un ZIP portátil, extrae los archivos a una carpeta estable antes de ejecutar `JanusDock.exe`.

> La aplicación no está firmada digitalmente; Windows SmartScreen puede pedir confirmación. Descarga solo desde los archivos oficiales de esta Release.

## Novedades desde 6.3.97

- **Opciones reorganizadas:** navegación lateral por **General, Apariencia, Accesos, Sistema y Acerca de**, tarjetas redondeadas y distribución adaptable de una o dos columnas. Los ajustes se previsualizan; **Cancelar** restaura los valores anteriores.
- **Barra e iconos:** celdas más compactas, separación configurable de **0 a 24 px**, resolución mejorada de iconos originales de Windows y accesos a Office/navegadores.
- **Efectos de puntero:** **Ninguno, Foco o Lupa**, junto con estilos de color **Windows, personalizado y clásico**; se evita reservar espacio adicional para la ampliación.
- **Botones del sistema:** **Bloquear, Reiniciar y Apagar**, con símbolos uniformes de Windows y cápsula redondeada independiente al extremo del Dock. Apagar y Reiniciar solicitan confirmación.
- **CPU y RAM:** mostrar por separado, con disposición **horizontal (lado a lado)** o **vertical (apilada)** y porcentajes actualizados cada segundo. Su opción está en **General → Información del sistema** o en el menú contextual del indicador.
- **Red:** monitor de tráfico con promedio móvil de 60 segundos; puede colocarse junto a los botones de energía **sin integrarse dentro de la cápsula**.
- **Discos:** indicadores por unidad, colores individuales, barra de uso y acceso a la unidad desde el icono. Configurables en **Sistema**.
- **Acerca de:** presentación en dos columnas, imagen monocromática integrada, ficha del proyecto y acceso a las actualizaciones.
- **Nombres de accesos:** se muestran al pasar el puntero, sin rótulos permanentes ocupando espacio en la barra.

## Características

- **Posición:** arriba, abajo, izquierda o derecha, en modo acoplado o flotante; puede reservar espacio para las ventanas mediante AppBar.
- **Accesos:** programas, archivos, carpetas, unidades, separadores y accesos habituales de Windows, navegadores y Office. Admite arrastrar archivos.
- **Organización:** cambiar el orden de los accesos y modificar el tamaño de iconos entre **16 y 96 px**; también con **Ctrl + rueda** sobre el Dock.
- **Discos:** iconos Windows o Lucide, colores configurables, ocupación y detalle de capacidad en el tooltip.
- **Indicadores:** tráfico de red, CPU y RAM, con actualización periódica y presentación compacta.
- **Personalización:** barra según los colores de Windows o con color propio; selectores, interruptores y tarjetas modernos.
- **Idiomas:** español de Chile, inglés y portugués de Brasil (algunas traducciones siguen en desarrollo).
- **Inicio y visibilidad:** mantener encima, ocultar automáticamente e iniciar con Windows.
- **Configuración local:** guardada en `%APPDATA%\JANUS\Dock\config.json`.

## Uso y actualizaciones

Haz **clic derecho sobre el fondo del Dock** para agregar elementos, organizar accesos, actualizar la barra o entrar a Opciones. En **General → Información del sistema** se configuran CPU, RAM, red y su posición; en **Sistema** se encuentran las unidades y sus colores.

En **Acerca de → Buscar actualizaciones en GitHub** se abre la última versión pública. JANUS Dock todavía no descarga ni instala actualizaciones automáticamente.

## Requisitos

- Windows **10 u 11, 64 bits**.
- La edición autónoma y el instalador incluyen .NET.
- La edición liviana necesita [Microsoft .NET 8 Desktop Runtime x64](https://dotnet.microsoft.com/download/dotnet/8.0).
- Normalmente no requiere privilegios de administrador para ejecutarse.

> .NET 8 finaliza su soporte en noviembre de 2026. Está prevista la evaluación de migración a una versión LTS posterior.

## Desarrollo

Proyecto en **C#**, **Windows Forms** y **.NET 8**. Para compilar en Windows con el SDK de .NET 8:

```powershell
dotnet restore
dotnet build JanusDock.csproj -c Release
```

Las compilaciones están en [GitHub Actions](https://github.com/momocrackcorp/janus-dock/actions). Los símbolos de Bloquear, Reiniciar y Apagar proceden de la fuente Windows **Segoe MDL2 Assets**; no se incluyen antiguos PNG de Flaticon.

- [Versiones y descargas](https://github.com/momocrackcorp/janus-dock/releases)
- [Código fuente](https://github.com/momocrackcorp/janus-dock)
- [Reportar un problema](https://github.com/momocrackcorp/janus-dock/issues)

---

<div align="center">

**Desarrollado por Momocrackcorp · Ñuble, Chile 🇨🇱**

</div>
