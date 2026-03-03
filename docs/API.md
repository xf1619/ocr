# OCR API 接口文档

## 基础信息

- Base URL：`http://127.0.0.1:8000`
- 接口版本：`v1`
- 数据格式：`application/json`（文件上传接口使用 `multipart/form-data`）

---

## 1) 健康检查

### `GET /health`

**描述**：检查服务存活状态。

**响应示例**

```json
{
  "status": "ok",
  "service": "deepseek-ocr",
  "max_upload_size_mb": 10
}
```

---

## 2) URL OCR

### `POST /api/v1/ocr/url`

**描述**：通过公网图片 URL 做 OCR。

**请求头**

- `Content-Type: application/json`

**请求参数**

| 字段 | 类型 | 必填 | 默认值 | 说明 |
|---|---|---|---|---|
| image_url | string | 是 | - | 公网可访问图片 URL |
| prompt | string | 否 | `请提取图片中的全部文字。` | OCR 指令 |

**请求示例**

```json
{
  "image_url": "https://example.com/test.png",
  "prompt": "请提取图片中的全部文字，并按段落输出。"
}
```

**成功响应示例（200）**

```json
{
  "model": "deepseek-ai/DeepSeek-OCR",
  "text": "这是识别出的文字内容...",
  "usage": {
    "prompt_tokens": 123,
    "completion_tokens": 45,
    "total_tokens": 168
  },
  "raw": {
    "id": "xxx",
    "choices": [
      {
        "message": {
          "content": "这是识别出的文字内容..."
        }
      }
    ]
  }
}
```

**失败响应示例（400）**

```json
{
  "detail": "上游接口错误：HTTP 401 - ..."
}
```

---

## 3) Base64 OCR

### `POST /api/v1/ocr/base64`

**描述**：通过 Base64 图片数据做 OCR。

**请求头**

- `Content-Type: application/json`

**请求参数**

| 字段 | 类型 | 必填 | 默认值 | 说明 |
|---|---|---|---|---|
| image_base64 | string | 是 | - | 图片 Base64，可带 `data:image/...;base64,` 前缀 |
| mime_type | string | 否 | `image/png` | 图片 MIME 类型 |
| prompt | string | 否 | `请提取图片中的全部文字。` | OCR 指令 |

**请求示例**

```json
{
  "image_base64": "iVBORw0KGgoAAAANSUhEUgAA...",
  "mime_type": "image/png",
  "prompt": "请提取图片中的全部文字"
}
```

**成功响应 / 失败响应**：结构同 URL OCR。

---

## 4) 文件上传 OCR

### `POST /api/v1/ocr/file`

**描述**：上传本地图片文件做 OCR。

**请求头**

- `Content-Type: multipart/form-data`

**表单字段**

| 字段 | 类型 | 必填 | 默认值 | 说明 |
|---|---|---|---|---|
| file | file | 是 | - | 图片文件 |
| prompt | string | 否 | `请提取图片中的全部文字。` | OCR 指令 |

**curl 示例**

```bash
curl -X POST 'http://127.0.0.1:8000/api/v1/ocr/file' \
  -F 'file=@./invoice.png' \
  -F 'prompt=请提取图片中的全部文字，保持原始排版'
```

**成功响应 / 失败响应**：结构同 URL OCR。

---

## 5) 错误码说明

| HTTP 状态码 | 含义 | 常见原因 |
|---|---|---|
| 200 | 调用成功 | - |
| 400 | 请求参数错误或上游调用失败 | 未配置密钥、图片无效/Base64 非法、上传了不支持的类型、文件大小超限、上游返回 4xx/5xx |
| 500 | 服务内部错误 | 程序异常、不可预期错误 |

---

## 6) 鉴权与安全建议

1. 本项目服务端通过环境变量读取 `SILICONFLOW_API_KEY`，不要将密钥暴露到前端。
2. 建议生产环境开启反向代理限流、请求大小限制、HTTPS。
3. 可结合业务增加鉴权（JWT/API Key）后再开放接口。

---

## 7) 扩展建议

- 增加批量 OCR 接口（多图并发）。
- 增加文档结构化解析（如票据字段提取）。
- 将 `raw` 输出持久化到数据库用于审计与追溯。
