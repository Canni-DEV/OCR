# Despliegue en Windows/IIS

## Publicación de la API .NET
1. Instalar .NET 8/9 SDK en el servidor.
2. Ejecutar `dotnet publish src/Ocr.Api/Ocr.Api.csproj -c Release -o publish` en la raíz de `ocr-system`.
3. Configurar un sitio en IIS apuntando a la carpeta `publish`. Habilitar modo **in-process** o **out-of-process** según política.
4. Establecer el pool de aplicaciones con arquitectura x64 y límites de request (por ejemplo `maxAllowedContentLength` acorde a uploads de 100 MB).
5. Copiar `appsettings.Production.json` con las secciones `Workers`, `TempStorage`, `Azure`, `Db`, `RateLimit`.
6. Habilitar logging con rollo diario y verificación de encabezado `X-Correlation-Id` en registros.

## Configuración de workers PaddleOCR
1. Instalar Python 3.10+ y dependencias con `pip install -r requirements.txt` en `src/Ocr.Worker.Paddle`.
2. Generar stubs gRPC (si no existen) ejecutando `..\scripts\generate_protos.ps1`.
3. Instalar NSSM o usar `sc.exe` para crear el servicio de Windows.
4. Ejecutar `run.ps1 -ServiceName OcrWorker1 -Port 50051 -WorkingDirectory C:\ocr-system\src\Ocr.Worker.Paddle`.
5. Repetir para cada worker cambiando `-Port` y actualizando `Workers:Endpoints` en `appsettings.Production.json`.

## Notas operativas
- Verificar conectividad saliente hacia Azure Cognitive Services antes de habilitar el fallback.
- La tabla `AzureUsage` controla el límite mensual; programar respaldos y supervisión.
- El worker es de concurrencia 1 por diseño para minimizar consumo de VRAM; escalar agregando instancias adicionales.
- Actualizar firewall para exponer únicamente los puertos gRPC internos y HTTPS público de la API.
