import base64
import binascii
from typing import Any, Dict

import httpx

from app.config import settings


class OCRClientError(RuntimeError):
    pass


class SiliconFlowOCRClient:
    def __init__(self) -> None:
        self.base_url = settings.siliconflow_base_url.rstrip("/")
        self.model = settings.siliconflow_model
        self.timeout = settings.request_timeout

    @property
    def api_key(self) -> str:
        # 每次请求时读取，支持运行中通过环境变量热更新
        return settings.siliconflow_api_key

    async def _request(self, payload: Dict[str, Any]) -> Dict[str, Any]:
        if not self.api_key:
            raise OCRClientError("未配置 SILICONFLOW_API_KEY，请在 .env 或环境变量中设置。")

        url = f"{self.base_url}/chat/completions"
        headers = {
            "Authorization": f"Bearer {self.api_key}",
            "Content-Type": "application/json",
        }

        try:
            async with httpx.AsyncClient(timeout=self.timeout) as client:
                resp = await client.post(url, headers=headers, json=payload)
        except httpx.HTTPError as e:
            raise OCRClientError(f"请求硅基流动失败: {e}") from e

        if resp.status_code >= 400:
            raise OCRClientError(f"上游接口错误：HTTP {resp.status_code} - {resp.text}")

        try:
            return resp.json()
        except ValueError as e:
            raise OCRClientError(f"上游返回非 JSON 响应: {resp.text}") from e

    def _extract_text(self, data: Dict[str, Any]) -> str:
        choices = data.get("choices") or []
        if not choices:
            return ""
        message = choices[0].get("message") or {}
        content = message.get("content")

        if isinstance(content, str):
            return content.strip()

        if isinstance(content, list):
            parts = []
            for item in content:
                if isinstance(item, dict) and item.get("type") == "text":
                    parts.append(item.get("text", ""))
            return "\n".join([p for p in parts if p]).strip()

        return ""

    async def recognize_by_image_url(self, image_url: str, prompt: str) -> Dict[str, Any]:
        payload = {
            "model": self.model,
            "messages": [
                {
                    "role": "user",
                    "content": [
                        {"type": "text", "text": prompt},
                        {"type": "image_url", "image_url": {"url": image_url}},
                    ],
                }
            ],
        }
        raw = await self._request(payload)
        return {
            "model": raw.get("model", self.model),
            "text": self._extract_text(raw),
            "usage": raw.get("usage"),
            "raw": raw,
        }

    async def recognize_by_base64(self, image_base64: str, mime_type: str, prompt: str) -> Dict[str, Any]:
        normalized = image_base64.strip()
        if normalized.startswith("data:"):
            image_data_url = normalized
        else:
            try:
                # 验证 Base64 合法性（失败会抛出异常）
                base64.b64decode(normalized, validate=True)
            except (binascii.Error, ValueError) as e:
                raise OCRClientError("image_base64 不是合法的 Base64 编码") from e
            image_data_url = f"data:{mime_type};base64,{normalized}"

        payload = {
            "model": self.model,
            "messages": [
                {
                    "role": "user",
                    "content": [
                        {"type": "text", "text": prompt},
                        {"type": "image_url", "image_url": {"url": image_data_url}},
                    ],
                }
            ],
        }
        raw = await self._request(payload)
        return {
            "model": raw.get("model", self.model),
            "text": self._extract_text(raw),
            "usage": raw.get("usage"),
            "raw": raw,
        }
