#!/usr/bin/env bash
# 아트 작업을 Codex에 위임한다. codex-art-brief.md(상시 지시서)를 요청 앞에 붙여 보낸다.
#
# 사용법:
#   Tools/codex-art.sh [옵션] "<요청 본문>"
#   Tools/codex-art.sh [옵션] -f <요청파일>
#
# 옵션:
#   -w         쓰기 허용. 실제로 PNG를 만들거나 바꿀 때 필요 (기본은 읽기 전용 조사·제안)
#   -u         Unity 연결까지 요청 (기본은 PNG까지)
#   -r         직전 세션 이어서
#   -m <MODEL> 모델 지정
#   -f <FILE>  요청 본문을 파일에서 읽는다
#   -n         Codex를 호출하지 않고 보낼 프롬프트만 출력한다 (검토용)
#   -v         Codex 진행 로그까지 보여준다
#
# 예:
#   Tools/codex-art.sh "인간 여성 걷기 8방향 제작 계획과 예상 비용을 조사해 제안만 해라."
#   Tools/codex-art.sh -w -u "인간 여성 걷기 8방향 × 8프레임. 캔버스 640×724, 발밑 (320,13)."

set -uo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BRIEF="$HERE/codex-art-brief.md"

WRITE=""
UNITY="PNG까지"
RESUME=""
MODEL=""
REQ_FILE=""
DRY=0
VERBOSE=""

while getopts "wurm:f:nv" opt; do
    case "$opt" in
        w) WRITE="-w" ;;
        u) UNITY="Unity 연결까지" ;;
        r) RESUME="-r" ;;
        m) MODEL="$OPTARG" ;;
        f) REQ_FILE="$OPTARG" ;;
        n) DRY=1 ;;
        v) VERBOSE="-v" ;;
        *) echo "알 수 없는 옵션: -$OPTARG" >&2; exit 2 ;;
    esac
done
shift $((OPTIND - 1))

if [ -n "$REQ_FILE" ]; then
    [ -f "$REQ_FILE" ] || { echo "요청 파일이 없습니다: $REQ_FILE" >&2; exit 2; }
    REQUEST="$(cat "$REQ_FILE")"
elif [ $# -ge 1 ]; then
    REQUEST="$1"
else
    echo "요청 본문이 필요합니다. -f <파일> 또는 \"<요청>\"" >&2
    exit 2
fi

[ -f "$BRIEF" ] || { echo "상시 지시서가 없습니다: $BRIEF" >&2; exit 2; }

PROMPT="$(cat <<EOF
너는 Underwrite 프로젝트의 아트 담당이다. 아래 상시 지시서를 먼저 읽고 그 규칙 안에서 작업해라.
상시 지시서가 참조하는 프로젝트 문서는 직접 열어서 확인해라. 요약을 신뢰하지 말고 원문을 본다.

작업 범위: ${UNITY}
쓰기 권한: $([ -n "$WRITE" ] && echo "있음 (workspace-write)" || echo "없음 (읽기 전용 — 조사와 제안만. 파일을 만들거나 바꾸지 마라)")

===== 상시 지시서 시작 =====
$(cat "$BRIEF")
===== 상시 지시서 끝 =====

===== 이번 요청 =====
${REQUEST}
===== 요청 끝 =====

상시 지시서 §4의 보고 양식으로 끝내라.
EOF
)"

if [ "$DRY" -eq 1 ]; then
    printf '%s\n' "$PROMPT"
    exit 0
fi

ARGS=()
[ -n "$WRITE" ] && ARGS+=("$WRITE")
[ -n "$RESUME" ] && ARGS+=("$RESUME")
[ -n "$VERBOSE" ] && ARGS+=("$VERBOSE")
[ -n "$MODEL" ] && ARGS+=(-m "$MODEL")

exec "$HERE/codex-run.sh" "${ARGS[@]+${ARGS[@]}}" "$PROMPT"
