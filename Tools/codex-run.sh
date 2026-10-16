#!/usr/bin/env bash
# Codex CLI를 비대화식으로 호출하는 래퍼. Claude Code가 Bash 도구로 호출한다.
#
# 사용법:
#   Tools/codex-run.sh [옵션] "<프롬프트>"
#   echo "<긴 프롬프트>" | Tools/codex-run.sh [옵션] -
#
# 옵션:
#   -w            쓰기 허용 (workspace-write). 기본은 읽기 전용
#   -r            직전 세션 이어서 질문 (codex exec resume --last)
#   -R <ID>       지정한 세션 ID를 이어서 질문
#   -m <MODEL>    모델 지정 (기본: ~/.codex/config.toml 값)
#   -d <DIR>      작업 루트 (기본: 이 스크립트의 상위 폴더)
#   -s <FILE>     출력 JSON Schema 파일 (--output-schema)
#   -v            Codex의 진행 로그를 그대로 보여준다 (기본: 최종 답변만)
#
# 출력: 최종 답변만 stdout으로. 진행 로그와 MCP 경고는 버린다.
# 종료 코드: Codex의 종료 코드를 그대로 전달.

set -uo pipefail

SANDBOX="read-only"
RESUME_MODE=""
RESUME_ID=""
MODEL=""
WORK_DIR=""
SCHEMA=""
VERBOSE=0

while getopts "wrR:m:d:s:v" opt; do
    case "$opt" in
        w) SANDBOX="workspace-write" ;;
        r) RESUME_MODE="last" ;;
        R) RESUME_MODE="id"; RESUME_ID="$OPTARG" ;;
        m) MODEL="$OPTARG" ;;
        d) WORK_DIR="$OPTARG" ;;
        s) SCHEMA="$OPTARG" ;;
        v) VERBOSE=1 ;;
        *) echo "알 수 없는 옵션: -$OPTARG" >&2; exit 2 ;;
    esac
done
shift $((OPTIND - 1))

if [ $# -lt 1 ]; then
    echo "프롬프트가 필요합니다. 사용법은 스크립트 머리 주석을 보세요." >&2
    exit 2
fi
PROMPT="$1"

# Codex는 Windows 바이너리이므로 넘기는 경로는 모두 Windows 형식이어야 한다.
# Git Bash의 /c/... 와 /tmp/... 는 그대로 쓰면 os error 123이 난다.
to_win() {
    if command -v cygpath >/dev/null 2>&1; then
        cygpath -w "$1"
    else
        echo "$1"
    fi
}

# 기본 작업 루트는 이 스크립트의 상위 폴더
if [ -z "$WORK_DIR" ]; then
    WORK_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
fi
WORK_DIR_WIN="$(to_win "$WORK_DIR")"

OUT_FILE="$(mktemp -t codex-out-XXXXXX)"
LOG_FILE="$(mktemp -t codex-log-XXXXXX)"
OUT_FILE_WIN="$(to_win "$OUT_FILE")"
trap 'rm -f "$OUT_FILE" "$LOG_FILE"' EXIT

ARGS=(exec)
if [ "$RESUME_MODE" = "last" ]; then
    ARGS+=(resume --last)
elif [ "$RESUME_MODE" = "id" ]; then
    ARGS+=(resume "$RESUME_ID")
fi

ARGS+=(--cd "$WORK_DIR_WIN" --sandbox "$SANDBOX" --skip-git-repo-check --color never -o "$OUT_FILE_WIN")
# 이어서 질문하는 경우가 아니면 세션을 저장하지 않는다.
# ~/.codex/thread_history_1.sqlite가 400MB를 넘어 세션 기록 쓰기가 느려질 수 있다.
if [ -z "$RESUME_MODE" ]; then
    ARGS+=(--ephemeral)
fi
[ -n "$MODEL" ] && ARGS+=(-m "$MODEL")
[ -n "$SCHEMA" ] && ARGS+=(--output-schema "$(to_win "$SCHEMA")")
ARGS+=("$PROMPT")

if [ "$VERBOSE" -eq 1 ]; then
    codex "${ARGS[@]}" </dev/null
    STATUS=$?
else
    codex "${ARGS[@]}" </dev/null >"$LOG_FILE" 2>&1
    STATUS=$?
fi

if [ -s "$OUT_FILE" ]; then
    cat "$OUT_FILE"
else
    echo "Codex가 최종 답변을 내지 않았습니다. 종료 코드: $STATUS" >&2
    echo "--- 로그 마지막 30줄 ---" >&2
    tail -30 "$LOG_FILE" >&2
fi

exit $STATUS
