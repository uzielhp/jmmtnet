# CobachJMMT – Blazor Web App (.NET 9, sin JavaScript propio)

Versión del proyecto Razor Pages reescrita con **Razor Components + Blazor Server interactivo**.
No hay ni una línea de JavaScript escrita a mano; solo el runtime que Blazor trae (`_framework/blazor.web.js`).

## Cómo ponerlo a funcionar
1. Copia del proyecto original a `wwwroot/` (sin cambios): `css/bootstrap.min.css`, `css/bootstrap-icons.min.css`,
   `fonts/`, `imgs/` (logo, favicon, adm.png, `galeria/`, `examen/`).
2. **No** copies `wwwroot/js/` (ya no se usa). `wwwroot/css/principal.css` ya viene actualizado.
3. `dotnet run`

## Qué reemplazó a cada JavaScript
| Antes (JS)                                   | Ahora (C#)                                                        |
|----------------------------------------------|-------------------------------------------------------------------|
| Menú desplegable / submenú (bootstrap.js)    | Estado `menuAbierto` / `submenuAbierto` en `MainLayout.razor`     |
| Envío del examen con `fetch`                 | `EnviarAsync()` en `Examen.razor`                                 |
| Barra de progreso del examen                 | Propiedad `Porcentaje` calculada                                  |
| Logo girando al enviar                       | `EstadoUi.Cargando`                                               |
| Componente Range (relleno del slider)        | Variable CSS `--bs-range-fill` calculada en `Act10.razor`         |
| Calculadora (formulario GET + botón)         | `@bind:event="oninput"`: se recalcula en vivo                     |
| `HttpContext.Session`                        | `SesionStore` + cookie anónima `cid`                              |
| `Pages/*.cshtml` + `.cshtml.cs`              | Un solo `.razor` por página                                       |

## Mejoras
- Respuestas correctas solo en memoria del servidor (nunca llegan al navegador).
- Envío con reserva atómica (no se puede enviar dos veces desde dos pestañas) y reintento si falla.
- Config de Apps Script con caché + bloqueo (no hay avalancha de peticiones) y falla recordada 5 s.
- Login admin: comparación por hash en tiempo constante y bloqueo 1 min tras 5 intentos.
- El admin no muestra el formulario si no pudo leer la config (evita guardar hojas en blanco).
- Se corrigió `label for="nombres"` de Apellidos; preguntas sin respuesta clave ya no se omiten al guardar.
- Galería con lista de archivos en caché; `MapStaticAssets` (compresión + fingerprint).
- Circuitos retenidos 10 min si el alumno pierde la red.

## Limitación importante
La comprobación **"bloquear el examen si el navegador del alumno tiene Internet"** no puede hacerse sin JavaScript:
solo el navegador puede intentar llegar a Google. Se eliminó. Opciones: bloquearlo a nivel de red/firewall
(lo más fiable) o un pequeño componente Blazor WebAssembly aparte.
