param(
    [int]$envs= 4,
    [string]$modelId="0",
    [bool]$force = $false,
    [bool]$exe = $false
)

$configurePath = "PLATEAUTutorial/Assets/Config/Tutorial-1.yaml"
$exePath = ""

if($exe) {
    if($force) {
        mlagents-learn $configurePath --env=$exePath --num-envs=$envs --run-id=$modelId --force --width=1920 --height=1080 --torch-device="cuda"
    } else {
        mlagents-learn $configurePath --env=$exePath --run-id=$modelId --num-envs=$envs --width=1920 --height=1080 --torch-device="cuda" --resume
    }
} else {
    if($force) {
        mlagents-learn $configurePath --run-id=$modelId --force --width=1920 --height=1080 --torch-device="cuda"
    } else {
        mlagents-learn $configurePath --run-id=$modelId --width=1920 --height=1080 --torch-device="cuda" --resume
    }
}
