#!/usr/bin/env python3
from pathlib import Path
import os
import shutil


SKILL_NAME = "kk-ui-umg"


def main() -> int:
    script_path = Path(__file__).resolve()
    skill_dir = script_path.parents[1]

    if skill_dir.name != SKILL_NAME or not (skill_dir / "SKILL.md").is_file():
        print(f"Error: install script must run from the {SKILL_NAME} skill directory.")
        return 1

    codex_home = Path(os.environ.get("CODEX_HOME", Path.home() / ".codex")).expanduser()
    target_root = codex_home / "skills"
    target_dir = target_root / SKILL_NAME
    target_root.mkdir(parents=True, exist_ok=True)

    if target_dir.exists():
        shutil.rmtree(target_dir)

    ignore = shutil.ignore_patterns("__pycache__", "*.pyc", ".DS_Store", "*.meta")
    shutil.copytree(skill_dir, target_dir, ignore=ignore)

    print(f"Installed {SKILL_NAME} to {target_dir}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
