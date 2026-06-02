import re
import json
import os

# 1. 망가진 원본 영어 성경 파일 로드
file_name = "bible_en.json"
if not os.path.exists(file_name):
    print(f"❌ 에러: {file_name} 파일을 찾을 수 없습니다. 스크립트와 같은 폴더에 두세요.")
    exit()

print("⏳ 성경 파일 분석 및 복구를 시작합니다...")
with open(file_name, "r", encoding="utf-8") as f:
    content = f.read()

lines = content.splitlines()
parsed_bible = {}
current_key = None
current_text = ""

# 성경 구절 시작 패턴 매칭 기법 활용 (예: "Genesis 1:1": "...)
verse_start_pattern = re.compile(r'^"([\d\s]*[A-Za-z\s]+ \d+:\d+)":\s*"(.*)')

for line in lines:
    line_str = line.strip()
    if not line_str or line_str in ("{", "}"):
        continue
    
    match = verse_start_pattern.match(line_str)
    if match:
        if current_key:
            parsed_bible[current_key] = current_text
        current_key = match.group(1)
        current_text = match.group(2)
    else:
        if current_key:
            current_text += " " + line_str

if current_key:
    parsed_bible[current_key] = current_text

# 2. 문장 내부의 잘못된 따옴표 잔재 및 깨진 공백 일괄 정제
cleaned_bible = {}
for key, text in parsed_bible.items():
    t = text.strip()
    if t.endswith(","): t = t[:-1].strip()
    if t.endswith('"'): t = t[:-1].strip()
    
    # 내부 침범 따옴표 완전 제거 및 다중 공백 정상화
    t = t.replace('"', '')
    t = re.sub(r'\s+', ' ', t).strip()
    cleaned_bible[key] = t

# 3. 표준 규격에 맞는 올바른 JSON 포맷으로 덮어쓰기 저장
with open(file_name, "w", encoding="utf-8") as f:
    json.dump(cleaned_bible, f, ensure_ascii=False, indent=2)

print("🎉 복구 완료! bible_en.json 파일이 완벽한 표준 JSON 규격으로 전면 수정되었습니다.")