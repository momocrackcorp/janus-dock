# JANUS Dock {{VERSION}}

Actualización estable para Windows 10 y 11 (64 bits).

## Novedades principales
- **Controles multimedia integrados en la barra:** anterior, reproducir/pausar, detener, siguiente y volumen − / +. Los controles se sincronizan con reproductores compatibles de Windows, incluido Spotify.
- **Información musical por 8 segundos:** muestra **canción y artista** al comenzar, reanudar o cambiar de pista, cuando el reproductor publica esos datos.
- **JANUS Media Bridge v2 opcional:** se descarga desde GitHub una sola vez al activar la función; actualizable desde Opciones. No se instala el Windows SDK ni se aumenta el tamaño del programa principal.
- **Reloj mejorado:** estilo LCD o texto, 12/24 h, AM/PM, fecha y calcomanía opcionales. Corregida su visibilidad al alternar el monitor de red.
- **Round corner al extremo derecho:** módulo compacto de energía y reloj, independiente del tráfico de red, sin separaciones con la barra.
- **Red, CPU y RAM:** tráfico de red con icono Lucide, flechas y selección del fondo; indicadores circulares CPU/RAM y vista clásica.
- **Terminal / CMD:** icono Lucide transparente, ordenable entre los accesos y separadores; configurado en Opciones.
- **General y Apariencia:** controles redistribuidos, gestión de accesos y personalización de barra.
- **Instalador y documentación** actualizados, manteniendo la configuración existente.

## Descargas
- **JANUS-Dock-Setup-{{VERSION}}-Autonomo.exe**: instalación o actualización recomendada, con .NET incluido.
- **JANUS-Dock-Autonomo-Windows-x64.zip**: edición portátil con .NET incluido.
- **JANUS-Dock-Liviano-Windows-x64.zip**: edición portátil para equipos que ya tienen .NET 8 Desktop Runtime x64.
- **JANUS-MediaBridge-v2.zip**: complemento multimedia opcional. No hace falta instalarlo manualmente si se usa el botón de Opciones.
- **SHA256SUMS.txt**: verificación SHA-256 de los cuatro archivos anteriores.

## Actualización desde v6.4.87
Cierra JANUS Dock, ejecuta el instalador y conserva tus ajustes en `%APPDATA%\JANUS\Dock\config.json`.
El componente multimedia se instala/actualiza por separado en `%LOCALAPPDATA%\JANUS\Dock\MediaBridge`, sin modificar DLL de Windows.
El instalador no exige desinstalar la edición previa.

**Nota:** JANUS Dock aún no actualiza la aplicación automáticamente; el botón «Buscar actualizaciones» abre GitHub. El reproductor es opcional y depende de la información que Windows reciba del programa de audio.

Momocrackcorp · Ñuble, Chile.
