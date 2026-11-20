# COBACH JMMT – ASP.NET Core Razor Pages

1. Copia tus archivos existentes a wwwroot:
   - css/bootstrap.min.css, css/bootstrap-icons.min.css
   - fonts/bootstrap-icons.woff2 (y .woff)
   - js/bootstrap.bundle.min.js
   - imgs/logo.png, imgs/adm.png, imgs/favicon.png
   - imgs/galeria/*.jpg  (ya no se necesita galeria.json ni generar_imgs.sh)
   - imgs/examen/1.jpg, 2.jpg ...
2. En css/bootstrap-icons.min.css la fuente se carga con "../fonts/..." → wwwroot/fonts.
3. dotnet run
