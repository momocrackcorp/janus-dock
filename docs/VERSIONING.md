# JANUS Dock — Política de versiones y publicación

Cada build de `Build JANUS Dock` recibe un número `N` de GitHub Actions. El workflow calcula automáticamente una versión `M.m.pp`:

- Para `100 <= N < 1000`: `M=6`, `m=3+floor(N/100)`, `pp=N%100` (dos dígitos). Ejemplo: #187=6.4.87, #211=6.5.11, #300=6.6.00.
- Para `N >= 1000`: `M=7+floor((N-1000)/1000)`, `m=floor(((N-1000)%1000)/100)`, `pp=N%100`. Ejemplo: #1000=7.0.00, #1100=7.1.00, #2000=8.0.00.

Así, #700 corresponde a 6.10.00, no a 7.0.00: se mantiene el hito 7.0 en #1000.

La fuente de verdad es `scripts/version-from-build.ps1`. GitHub Actions pasa esta versión a `Version`, `AssemblyVersion` y `FileVersion` en las dos ediciones; el panel «Acerca de» lee la versión compilada.

## Publicaciones

1. Los commits normales en `main` se compilan, pero **no publican Releases**. Se pueden descargar temporalmente desde GitHub Actions.
2. El commit de promoción debe incluir **`[publish-release]`** en su mensaje; únicamente si el workflow **Build JANUS Dock** termina exitoso y es de `main`, `auto-release.yml` publica.
3. Publicación con el **mismo SHA y número de build validado**: descarga los ejecutables de ese run, verifica la versión, genera el instalador con Inno Setup y descarga/valida Media Bridge v2 por SHA-256.
4. Genera los ZIP portátiles, instalador, complemento multimedia y `SHA256SUMS.txt`; crea `vM.m.pp` como Release más reciente en GitHub.
5. Si falla una compilación, **no publica**. Si el tag ya existe, **no sobrescribe** la Release anterior.
6. Una publicación no implica distribución automática de la nueva aplicación a los equipos; el botón de actualizaciones abre GitHub. El componente multimedia sí puede descargarse por separado desde Opciones.

No editar la versión a mano como sustituto del número de Build; al realizar una nueva versión, publicar únicamente una vez que se hayan revisado los cambios.
