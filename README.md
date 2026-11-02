<div align="center">

# JANUS Dock

### Barra de accesos, indicadores y controles multimedia para Windows

**Windows 10 / 11 · x64 · C# · Windows Forms · .NET 8**

[![Descargar JANUS Dock v6.5.11](https://img.shields.io/badge/Descargar-v6.5.11-2563eb?style=for-the-badge&logo=github)](https://github.com/momocrackcorp/janus-dock/releases/tag/v6.5.11)
[![Compilación](https://github.com/momocrackcorp/janus-dock/actions/workflows/build-janus-dock.yml/badge.svg)](https://github.com/momocrackcorp/janus-dock/actions/workflows/build-janus-dock.yml)

</div>

---

**JANUS Dock** es una barra independiente para Windows 10 y 11. Nació como una alternativa configurable ante las restricciones de ubicación de la barra de tareas de Windows 11. Permite abrir y ordenar programas y carpetas, acceder a unidades, ver indicadores de rendimiento y controlar música sin dejar el escritorio.

## Versión estable: 6.5.11 (compilación #211)

**[Descargar JANUS Dock v6.5.11](https://github.com/momocrackcorp/janus-dock/releases/tag/v6.5.11)**

| Archivo | Uso |
| --- | --- |
| `JANUS-Dock-Setup-6.5.11-Autonomo.exe` | **Recomendado**: instalador x64, .NET incluido, actualiza conservando ajustes |
| `JANUS-Dock-Autonomo-Windows-x64.zip` | Portátil, incluye .NET |
| `JANUS-Dock-Liviano-Windows-x64.zip` | Portátil pequeño, necesita .NET 8 Desktop Runtime x64 instalado |
| `JANUS-MediaBridge-v2.zip` | **Complemento opcional** para conocer canción, artista y estado multimedia |
| `SHA256SUMS.txt` | Comprobación SHA-256 de todos los archivos de la Release |

**Actualizar:** cierra el Dock y ejecuta el instalador. Se preserva `%APPDATA%\JANUS\Dock\config.json`; no tienes que desinstalar la versión anterior. No reemplaces ni borres DLL de Windows. El instalador puede solicitar confirmación de SmartScreen porque el programa no está firmado digitalmente.

## Novedades de la v6.5.11

- **Reproductor en la barra:** anterior, **Play/Pausa**, detener, siguiente, volumen − y volumen +, en un conjunto compacto. Reproducción y volumen mediante controles multimedia de Windows.
- **Título y artista:** muestra «Canción — Artista» durante **8 segundos** al iniciar, reanudar o cambiar de pista, cuando el reproductor comparte los datos con Windows.
- **JANUS Media Bridge v2 opcional:** instalación/actualización desde **Opciones → General → Botones del sistema**. El complemento se descarga desde nuestra Release de GitHub, se comprueba con SHA-256 y se almacena en `%LOCALAPPDATA%\JANUS\Dock\MediaBridge\v2`. Se descarga una vez y el Dock principal permanece liviano. Si tienes v1, puedes actualizar a v2 desde la misma opción. El SDK de desarrollo no se instala en tu PC.
- **Reloj:** digital LCD estilo Casio retro o solo texto; 12/24 h, DD-MM en la esquina superior del LCD, AM y PM con indicador activo resaltado en modo 12 h y calcomanía opcional reducida y centrada bajo el LCD. Corregida la aparición del reloj al ocultar o mostrar el tráfico de red.
- **Round corner:** cápsula redondeada de energía y reloj en el extremo derecho, integrada al alto de la barra; tráfico de red independiente a su izquierda.
- **Tráfico de red:** modo «Texto + icono Lucide» o «Gráfico», velocidad de subida/bajada, fondo oscuro o mismo color de barra.
- **CPU y RAM:** medidores circulares o clásicos, independientes y compactos.
- **Terminal/CMD:** icono Lucide integrado al color de la barra, movible en **Apariencia → Organizar accesos**, admite separadores antes y después.
- **Accesos, discos y exploración:** organización, separadores, iconos Windows/Lucide, consumo y colores por unidad. Nuevo árbol de carpetas en cascada: clic en «Este equipo» → «C:» → «Program Files» → otra carpeta. Funciona también con los accesos a carpetas y los indicadores de unidades. Las subcarpetas se cargan al abrir cada nivel, sin bloquear la barra; para entrar en la carpeta elegida, pulsa «Abrir esta carpeta en el Explorador». Los archivos aparecen al final y pueden abrirse con su aplicación predeterminada. El clic derecho conserva «Abrir» con su comportamiento anterior.

## Personalización

Elige barra horizontal (arriba/abajo), vertical (izquierda/derecha), acoplada o flotante. Configura iconos de 16 a 96 px, espaciado, fondo, modo de nombres, efectos Ninguno / Foco / Lupa, visibilidad, inicio con Windows y botones de energía. Las opciones tienen páginas **General, Apariencia, Accesos, Sistema y Acerca de**.

Los cambios de la barra pueden previsualizarse y Cancelar restaura la configuración anterior. Para mover Terminal, otros accesos o separadores usa **Apariencia → Organizar accesos**.

### Cómo habilitar el reproductor

1. Abre **Opciones → General → Botones del sistema**.
2. Activa **Mostrar controles multimedia**.
3. Acepta la descarga opcional del componente desde GitHub (o pulsa **Descargar / Actualizar componente multimedia**).
4. Utiliza los seis controles. Spotify y otros reproductores compatibles pueden informar canción, artista y estado. Si no proporcionan artista, se muestra solo la canción.

Si no activas el reproductor, **no se descarga el complemento**. No es necesario ingresar credenciales de Spotify. Puedes encontrar el mismo archivo en la Release v6.5.11 o en la Release independiente [JANUS Media Bridge v2](https://github.com/momocrackcorp/janus-dock/releases/tag/media-support-v2).

## Versiones automáticas y publicaciones

**La versión se deriva del número de compilación de GitHub Actions**, no se asigna manualmente en cada cambio. Ejemplos:

| Compilación | Versión |
| ---: | --- |
| #187 | 6.4.87 |
| #208 | 6.5.08 |
| #211 | 6.5.11 |
| #300 | 6.6.00 |
| #1000 | 7.0.00 |

Las compilaciones ordinarias generan artefactos de prueba. **Solo un commit marcado `[publish-release]`**, después de pasar ambas compilaciones de Windows, publica automáticamente una Release con instalador, las dos variantes portátiles, complemento multimedia opcional y SHA-256. Esto evita publicar versiones de prueba por accidente. La Release más reciente se consulta desde «Acerca de → Buscar actualizaciones en GitHub». **El Dock no realiza todavía actualizaciones automáticas del ejecutable.**

La regla completa se documenta en [VERSIONING.md](docs/VERSIONING.md).

## Requisitos y desarrollo

- Windows 10/11 de 64 bits.
- Instalador y edición Autónoma: incluyen .NET 8.
- Edición Liviana: requiere [Microsoft .NET 8 Desktop Runtime x64](https://dotnet.microsoft.com/download/dotnet/8.0).
- El complemento Media Bridge es opcional; se utiliza con reproductores que publiquen una sesión multimedia en Windows.
- .NET 8 termina su soporte en noviembre de 2026; se estudiará migrar a una LTS posterior.

Para compilar en Windows con el SDK de .NET 8:

```powershell
dotnet restore JanusDock.csproj
dotnet build JanusDock.csproj -c Release
```

Los binarios generados se distribuyen en **GitHub Releases**, no se guardan en el repositorio de código. Se conservaron el instalador Inno Setup, los recursos gráficos y la fotografía de «Acerca de». Se retiraron workflows de publicación antiguos para impedir versiones duplicadas.

[Releases](https://github.com/momocrackcorp/janus-dock/releases) · [Código fuente](https://github.com/momocrackcorp/janus-dock) · [Problemas y sugerencias](https://github.com/momocrackcorp/janus-dock/issues)

---

<div align="center">

**Momocrackcorp · Ñuble, Chile 🇨🇱**  
Autoría: Omar Aguila

</div>
