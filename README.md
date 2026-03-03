# DeepSeek-OCR 服务（Python + FastAPI + Web UI）

基于硅基流动（SiliconFlow）提供的 DeepSeek-OCR 模型，实现：

- OCR API（URL / Base64 / 文件上传）
- Web 前端 UI（上传图片后识别）
- 详细接口文档（`docs/API.md`）

## 1. 环境准备

```bash
python -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
```

复制环境变量模板并填写密钥：

```bash
cp .env.example .env
```

必须设置：

- `SILICONFLOW_API_KEY`

可选：

- `MAX_UPLOAD_SIZE_MB`（默认 10）

## 2. 启动服务

```bash
uvicorn app.main:app --host 0.0.0.0 --port 8000 --reload
```

启动后访问：

- 前端页面：`http://127.0.0.1:8000/`
- Swagger：`http://127.0.0.1:8000/docs`
- ReDoc：`http://127.0.0.1:8000/redoc`

## 3. 快速调用示例

### 3.1 文件上传 OCR

```bash
curl -X POST 'http://127.0.0.1:8000/api/v1/ocr/file' \
  -F 'file=@./demo.png' \
  -F 'prompt=请提取图片中的全部文字'
```

### 3.2 URL OCR

```bash
curl -X POST 'http://127.0.0.1:8000/api/v1/ocr/url' \
  -H 'Content-Type: application/json' \
  -d '{
    "image_url": "https://example.com/demo.png",
    "prompt": "请提取图片中的全部文字"
  }'
```

### 3.3 Base64 OCR

```bash
curl -X POST 'http://127.0.0.1:8000/api/v1/ocr/base64' \
  -H 'Content-Type: application/json' \
  -d '{
    "image_base64": "iVBORw0KGgoAAAANSUhEUg...",
    "mime_type": "image/png",
    "prompt": "请提取图片中的全部文字"
  }'
```


> 说明：`/api/v1/ocr/file` 仅允许常见图片类型（PNG/JPEG/WEBP/GIF/BMP），并限制上传大小（默认 10MB，可通过 `MAX_UPLOAD_SIZE_MB` 配置）；`/api/v1/ocr/url` 会校验 URL 格式，`/api/v1/ocr/base64` 会校验 Base64 合法性。

## 4. 目录结构

```text
.
├── app
│   ├── config.py
│   ├── main.py
│   ├── schemas.py
│   ├── services
│   │   └── ocr_client.py
│   └── static
│       └── index.html
├── docs
│   └── API.md
├── .env.example
├── requirements.txt
└── README.md
```
