#!/usr/bin/env python3
"""Compile against installed Unity APIs without executing the Unity editor.

This catches C# and API errors; it does NOT replace Unity import/build/play tests.
Uses only the local .NET SDK and the assemblies bundled with Unity.
"""
import argparse
from pathlib import Path
import os
import subprocess
import tempfile

parser = argparse.ArgumentParser()
parser.add_argument('--unity', default='/Applications/6000.3.5f2/Unity.app')
args = parser.parse_args()
root = Path(__file__).resolve().parents[1]
unity = Path(args.unity)
base = unity / 'Contents/Resources/Scripting'
sdks = subprocess.check_output(['dotnet', '--list-sdks'], text=True).splitlines()
version, location = sdks[-1].split(' ', 1)
csc = Path(location.strip('[]')) / version / 'Roslyn/bincore/csc.dll'
ui = list((unity / 'Contents/Resources/PackageManager/ProjectTemplates/libcache').glob('com.unity.template.2d-*/ScriptAssemblies/UnityEngine.UI.dll'))
if not ui:
    parser.error('No bundled UnityEngine.UI.dll was found. Use Unity to import uGUI first.')
refs = list((base / 'NetStandard/ref/2.1.0').glob('*.dll'))
refs += list((base / 'Managed/UnityEngine').glob('*.dll'))
refs += [ui[0]]
sources = sorted((root / 'Assets/Uninvited').rglob('*.cs'))
with tempfile.TemporaryDirectory(prefix='uninvited-compile-') as output:
    response = Path(output) / 'compile.rsp'
    response.write_text('\n'.join([
        '-nostdlib+', '-target:library', '-langversion:9.0',
        '-out:"' + str(Path(output) / 'Uninvited.Check.dll') + '"',
        *['-r:"' + str(p) + '"' for p in refs],
        *['"' + str(p) + '"' for p in sources],
    ]))
    env = dict(os.environ, DOTNET_CLI_HOME='/private/tmp/uninvited-dotnet', DOTNET_CLI_TELEMETRY_OPTOUT='1')
    subprocess.run(['dotnet', str(csc), '@' + str(response)], env=env, check=True)
print('C# compile check passed. Unity import, scene execution and Web build remain separate checks.')
