from typing import Any, Dict, Optional

from pydantic import BaseModel, Field, HttpUrl


class OCRUrlRequest(BaseModel):
    image_url: HttpUrl = Field(..., description="公网可访问的图片 URL")
    prompt: str = Field(default="请提取图片中的全部文字。", description="OCR 提示词")


class OCRBase64Request(BaseModel):
    image_base64: str = Field(..., description="Base64 编码图片字符串（可带 data:image/...;base64 前缀）")
    mime_type: str = Field(default="image/png", description="图片 MIME 类型")
    prompt: str = Field(default="请提取图片中的全部文字。", description="OCR 提示词")


class OCRResponse(BaseModel):
    model: str
    text: str
    usage: Optional[Dict[str, Any]] = None
    raw: Dict[str, Any]


class ErrorResponse(BaseModel):
    detail: str
