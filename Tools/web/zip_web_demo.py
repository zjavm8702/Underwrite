"""
웹 데모 빌드(Builds/WebDemo)를 올릴 zip으로 묶는다. index.html이 zip 맨 위에 오고, 경로는 / 로 쓴다.

실행: python Tools/web/zip_web_demo.py
"""
import os
import zipfile

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
SOURCE = os.path.join(ROOT, "Builds", "WebDemo")
TARGET = os.path.join(ROOT, "Builds", "Underwrite_WebDemo.zip")


def main():
    if os.path.exists(TARGET):
        os.remove(TARGET)

    with zipfile.ZipFile(TARGET, "w", zipfile.ZIP_DEFLATED) as archive:
        for folder, dirs, files in os.walk(SOURCE):
            # 버스트 디버그 정보는 올리지 않는다(DoNotShip).
            dirs[:] = [d for d in dirs if not d.endswith("DoNotShip")]
            for name in files:
                path = os.path.join(folder, name)
                archive.write(path, os.path.relpath(path, SOURCE).replace(os.sep, "/"))

    print(TARGET, os.path.getsize(TARGET) // (1024 * 1024), "MB")


if __name__ == "__main__":
    main()
