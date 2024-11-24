from fastapi import FastAPI, Request
import uvicorn
import json

app = FastAPI()

@app.middleware("http")
async def log_requests(request: Request, call_next):
    print(f"Incoming request: {request.method} {request.url}")
    response = await call_next(request)
    return response
    
@app.post("/")
async def root(request: Request):
    data = b""
    async for chunk in request.stream():
        data += chunk
    try:
        json_data = json.loads(data.decode())
        pretty_json = json.dumps(json_data, indent=4)
        print(pretty_json)
    except json.JSONDecodeError as e:
        print(f"Error decoding JSON: {e}")
    return {"status": "ok"}

if __name__ == "__main__":
    uvicorn.run(app, host="127.0.0.1", port=8000)
