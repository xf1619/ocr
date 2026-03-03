import base64

from fastapi import FastAPI, File, HTTPException, UploadFile
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import FileResponse
from fastapi.staticfiles import StaticFiles

from app.config import settings
from app.schemas import ErrorResponse, OCRBase64Request, OCRResponse, OCRUrlRequest
from app.services.ocr_client import OCRClientError, SiliconFlowOCRClient

app = FastAPI(
    title="DeepSeek-OCR Service (SiliconFlow)",
    version="1.1.0",
    description="使用硅基流动 DeepSeek-OCR 的 OCR 识别服务，提供 Web UI 与 REST API。",
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

app.mount("/static", StaticFiles(directory="app/static"), name="static")

ocr_client = SiliconFlowOCRClient()
ALLOWED_IMAGE_TYPES = {"image/png", "image/jpeg", "image/jpg", "image/webp", "image/gif", "image/bmp"}
MAX_UPLOAD_SIZE_BYTES = settings.max_upload_size_mb * 1024 * 1024


@app.get("/", include_in_schema=False)
async def index() -> FileResponse:
    return FileResponse("app/static/index.html")


@app.get("/health", tags=["System"])
async def health() -> dict:
    return {"status": "ok", "service": "deepseek-ocr", "max_upload_size_mb": settings.max_upload_size_mb}


@app.post(
    "/api/v1/ocr/url",
    response_model=OCRResponse,
    responses={400: {"model": ErrorResponse}, 500: {"model": ErrorResponse}},
    tags=["OCR"],
    summary="通过图片 URL 进行 OCR",
)
async def ocr_by_url(req: OCRUrlRequest):
    try:
        return await ocr_client.recognize_by_image_url(str(req.image_url), req.prompt)
    except OCRClientError as e:
        raise HTTPException(status_code=400, detail=str(e)) from e
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"服务内部错误: {e}") from e


@app.post(
    "/api/v1/ocr/base64",
    response_model=OCRResponse,
    responses={400: {"model": ErrorResponse}, 500: {"model": ErrorResponse}},
    tags=["OCR"],
    summary="通过 Base64 图像进行 OCR",
)
async def ocr_by_base64(req: OCRBase64Request):
    try:
        return await ocr_client.recognize_by_base64(req.image_base64, req.mime_type, req.prompt)
    except OCRClientError as e:
        raise HTTPException(status_code=400, detail=str(e)) from e
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"服务内部错误: {e}") from e


@app.post(
    "/api/v1/ocr/file",
    response_model=OCRResponse,
    responses={400: {"model": ErrorResponse}, 500: {"model": ErrorResponse}},
    tags=["OCR"],
    summary="通过文件上传进行 OCR",
)
async def ocr_by_file(file: UploadFile = File(...), prompt: str = "请提取图片中的全部文字。"):
    try:
        raw = await file.read()
        if not raw:
            raise HTTPException(status_code=400, detail="上传文件为空")
        if len(raw) > MAX_UPLOAD_SIZE_BYTES:
            raise HTTPException(status_code=400, detail=f"文件大小超限，最大 {settings.max_upload_size_mb}MB")

        mime_type = (file.content_type or "image/png").lower()
        if mime_type not in ALLOWED_IMAGE_TYPES:
            raise HTTPException(status_code=400, detail=f"不支持的图片类型: {mime_type}")

        encoded = base64.b64encode(raw).decode("utf-8")
        return await ocr_client.recognize_by_base64(encoded, mime_type, prompt)
    except OCRClientError as e:
        raise HTTPException(status_code=400, detail=str(e)) from e
    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"服务内部错误: {e}") from e
