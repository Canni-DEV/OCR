import asyncio
import logging
import os
import subprocess
import sys
import time
from pathlib import Path

import grpc

try:
    import ocrworker_pb2  # type: ignore
    import ocrworker_pb2_grpc  # type: ignore
except ImportError:  # pragma: no cover - ensures stubs can be generated on first run
    base_dir = Path(__file__).resolve().parents[2]
    proto_path = base_dir / "proto" / "ocrworker.proto"
    if proto_path.exists():
        subprocess.check_call(
            [
                sys.executable,
                "-m",
                "grpc_tools.protoc",
                f"-I{proto_path.parent}",
                f"--python_out={Path(__file__).parent}",
                f"--grpc_python_out={Path(__file__).parent}",
                str(proto_path),
            ]
        )
        import ocrworker_pb2  # type: ignore
        import ocrworker_pb2_grpc  # type: ignore
    else:
        raise

logger = logging.getLogger(__name__)


class OcrWorkerService(ocrworker_pb2_grpc.OcrWorkerServicer):
    def __init__(self, lang: str, use_angle_cls: bool, concurrency: int = 1):
        self.lang = lang
        self.use_angle_cls = use_angle_cls
        self._semaphore = asyncio.Semaphore(concurrency)
        self._paddle = None

    async def Ping(self, request, context):  # noqa: N802
        message = f"pong: {request.message}" if request.message else "pong"
        return ocrworker_pb2.PingResponse(message=message)

    async def ExtractText(self, request, context):  # noqa: N802
        start = time.perf_counter()
        async with self._semaphore:
            if not request.file_path:
                return ocrworker_pb2.ExtractTextResponse(ok=False, error="file_path is required")

            path = Path(request.file_path)
            if not path.exists():
                return ocrworker_pb2.ExtractTextResponse(ok=False, error="file not found")

            try:
                text = await asyncio.to_thread(self._run_paddle, str(path))
                elapsed_ms = int((time.perf_counter() - start) * 1000)
                return ocrworker_pb2.ExtractTextResponse(ok=True, text=text, elapsed_ms=elapsed_ms)
            except Exception as exc:  # pragma: no cover - defensive logging
                logger.exception("PaddleOCR execution failed")
                elapsed_ms = int((time.perf_counter() - start) * 1000)
                return ocrworker_pb2.ExtractTextResponse(ok=False, error=str(exc), elapsed_ms=elapsed_ms)

    def _run_paddle(self, file_path: str) -> str:
        if self._paddle is None:
            try:
                from paddleocr import PaddleOCR  # type: ignore
            except ImportError as exc:  # pragma: no cover
                raise RuntimeError("paddleocr is not installed") from exc

            self._paddle = PaddleOCR(lang=self.lang, use_angle_cls=self.use_angle_cls)

        assert self._paddle is not None
        result = self._paddle.ocr(file_path)
        lines = []
        for page in result:
            for line, _ in page:
                lines.append(line)
        return "\n".join(lines)


def get_env_bool(name: str, default: bool = False) -> bool:
    value = os.getenv(name)
    if value is None:
        return default
    return value.lower() in {"1", "true", "yes", "y", "on"}


def get_settings():
    return {
        "port": int(os.getenv("WORKER_PORT", "50051")),
        "lang": os.getenv("WORKER_LANG", "es"),
        "use_angle_cls": get_env_bool("PADDLE_USE_ANGLE_CLS", True),
        "concurrency": int(os.getenv("WORKER_CONCURRENCY", "1")),
    }


async def serve():
    logging.basicConfig(level=logging.INFO)
    settings = get_settings()
    server = grpc.aio.server(options=[("grpc.max_send_message_length", 20 * 1024 * 1024)])
    ocrworker_pb2_grpc.add_OcrWorkerServicer_to_server(
        OcrWorkerService(settings["lang"], settings["use_angle_cls"], settings["concurrency"]),
        server,
    )

    listen_addr = f"0.0.0.0:{settings['port']}"
    server.add_insecure_port(listen_addr)
    logger.info("Starting Paddle worker on %s", listen_addr)
    await server.start()
    await server.wait_for_termination()


def main():
    asyncio.run(serve())


if __name__ == "__main__":
    main()
