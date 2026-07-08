#!/usr/bin/env python3
from pathlib import Path
import sys


SKILL_NAME = "kk-ui-umg"
REQUIRED_FILES = [
    "SKILL.md",
    "references/schema-v054.md",
    "references/authoring-checklist.md",
    "references/examples.md",
    "references/business-adapter.md",
    "references/uimanager-runtime.md",
    "scripts/install_skill.py",
]
REQUIRED_TEXT = [
    "name: kk-ui-umg",
    "Assets/UI/Source/<PackageId>/",
    "Source package roots must stay under",
    "layout.json",
    "bindings.json",
    "codegen.json",
    "Runtime Setup And UIManager",
    "KK.UI.UMG.UIManager",
    "UIManager.Instance.OpenAsync",
    "OpenAsync(string systemId",
    "RegisterService<T>",
    "Static UI copy uses",
    "dynamic Store fields",
    "locKey",
    "Generated Parent Folder",
    "<Generated Parent>/<PackageId>/<PackageId>Controller.cs",
]


def resolve_skill_dir() -> Path:
    script_path = Path(__file__).resolve()
    if script_path.parent.name == "scripts":
        return script_path.parents[1]
    return script_path.parent


def main() -> int:
    skill_dir = resolve_skill_dir()
    errors = []

    if skill_dir.name != SKILL_NAME:
        errors.append(f"Expected skill directory name '{SKILL_NAME}', got '{skill_dir.name}'.")

    for relative in REQUIRED_FILES:
        if not (skill_dir / relative).is_file():
            errors.append(f"Missing required file: {relative}")

    skill_file = skill_dir / "SKILL.md"
    if skill_file.is_file():
        content = skill_file.read_text(encoding="utf-8")
        for text in REQUIRED_TEXT:
            if text not in content:
                errors.append(f"SKILL.md missing required text: {text}")

    if errors:
        for error in errors:
            print(f"Error: {error}", file=sys.stderr)
        return 1

    print(f"{SKILL_NAME} quick validation passed: {skill_dir}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
