# OCR System

Solución de referencia que combina una API REST en .NET 8/9 con un worker gRPC basado en PaddleOCR.

## Estructura
- `ocr-system/ocr-system.sln`: solución con la API.
- `ocr-system/src/Ocr.Api`: Web API con Swagger, clientes gRPC/Azure y servicios de post-procesamiento.
- `ocr-system/proto/ocrworker.proto`: contrato gRPC compartido.
- `ocr-system/src/Ocr.Worker.Paddle`: servidor gRPC en Python para PaddleOCR.
- `ocr-system/scripts`: utilidades para regenerar stubs.
- `ocr-system/docs`: guías de despliegue y SQL inicial.

## Regenerar stubs gRPC
```
cd ocr-system
bash scripts/generate_protos.sh
```

En Windows usar `scripts/generate_protos.ps1`.
