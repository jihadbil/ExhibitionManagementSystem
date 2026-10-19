#!/usr/bin/env python3
"""Run backend regressions against a disposable, loopback-only SQL Server."""
from pathlib import Path
import os, secrets, subprocess, sys, time

root = Path(__file__).resolve().parents[1]
docker = ['docker', '--host=unix:///var/run/docker.sock']
name = 'backend-regression-' + secrets.token_hex(6)
env = os.environ.copy()
env['MSSQL_SA_PASSWORD'] = secrets.token_urlsafe(32) + 'aA1!'
image = env.get('BACKEND_TEST_SQL_IMAGE', 'mcr.microsoft.com/mssql/server@sha256:cf8f0ab2ebe61feec2eed2629061a07d247f791c2a8ddda74a4ac9f06a291718')
try:
    subprocess.run(docker + ['run', '-d', '--name', name, '--label', 'purpose=backend-regression',
        '--memory', '3g', '--cpus', '2', '-e', 'ACCEPT_EULA=Y', '-e', 'MSSQL_PID=Developer',
        '-e', 'MSSQL_MEMORY_LIMIT_MB=1536', '-e', 'MSSQL_SA_PASSWORD', '-p', '127.0.0.1::1433', image],
        env=env, check=True, stdout=subprocess.DEVNULL)
    for attempt in range(45):
        check = subprocess.run(docker + ['exec', '-e', 'SQLCMDPASSWORD', name,
            '/opt/mssql-tools18/bin/sqlcmd', '-S', 'localhost', '-U', 'sa', '-C', '-l', '2', '-Q', 'SELECT 1'],
            env={**env, 'SQLCMDPASSWORD': env['MSSQL_SA_PASSWORD']}, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        if check.returncode == 0:
            break
        time.sleep(2)
    else:
        raise RuntimeError('Disposable SQL Server did not become ready')
    binding = subprocess.check_output(docker + ['port', name, '1433/tcp'], text=True).strip()
    port = int(binding.rsplit(':', 1)[1])
    env['BACKEND_TEST_CONNECTION'] = ('Server=127.0.0.1,' + str(port) + ';Database=BackendRegression_' + secrets.token_hex(6)
        + ';User Id=sa;Password=' + env['MSSQL_SA_PASSWORD'] + ';Encrypt=True;TrustServerCertificate=True')
    # TrustServerCertificate applies only to this disposable local server.
    command = ['dotnet', 'test', str(root / 'tests/Backend.RegressionTests/Backend.RegressionTests.csproj')]
    command.extend(sys.argv[1:])
    result = subprocess.run(command, cwd=root, env=env)
    sys.exit(result.returncode)
finally:
    subprocess.run(docker + ['rm', '-f', name], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
