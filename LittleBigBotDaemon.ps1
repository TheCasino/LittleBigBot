$loop = 1;
"LittleBigBot daemon v1.0 starting..."
while ($loop)
{
    $process = Start-Process "LittleBigBot.exe" -Wait -NoNewWindow -PassThru
    switch ($process.ExitCode)
    {
        0 {"[$(Get-Date)] Exit code 0 - Exiting..."; $loop = 0;}
        1 {"[$(Get-Date)] Exit code 1 - Restarting..."; Start-Sleep -s 3}
        default {"[$(Get-Date)] Unhandled exit code $($process.ExitCode) - exiting..."; $loop = 0}
    }
}
