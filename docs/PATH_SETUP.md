# Adding Reshape to Your PATH

After installing Reshape CLI, you need to ensure the installation directory is in your system PATH to use the `reshape` command from anywhere.

## Automatic PATH Setup (Recommended)

The installation scripts now automatically add Reshape to your PATH. However, you'll need to **restart your terminal** for the changes to take effect.

### If Using the Install Script

#### Windows (PowerShell)
```powershell
iex "& { $(irm https://raw.githubusercontent.com/jomaxso/Reshape/main/eng/scripts/install.ps1) }"
```

The script will automatically:
1. Install reshape to `$env:USERPROFILE\.reshape\bin`
2. Add that directory to your user PATH
3. Make it available immediately in the current session

**After installation, restart your terminal** for the PATH changes to be permanent.

#### Linux / macOS
```bash
curl -fsSL https://raw.githubusercontent.com/jomaxso/Reshape/main/eng/scripts/install.sh | bash
```

The script installs to `~/.local/bin` by default. Follow the on-screen instructions to add it to your PATH in your shell profile.

## Manual PATH Setup

If you installed Reshape manually or the automatic setup didn't work, follow these steps:

### Windows

#### Option 1: PowerShell (User PATH - Recommended)
```powershell
# Add to user PATH permanently
$installDir = "$env:USERPROFILE\.reshape\bin"
$userPath = [Environment]::GetEnvironmentVariable("Path", "User")
[Environment]::SetEnvironmentVariable("Path", "$userPath;$installDir", "User")

# Add to current session
$env:PATH += ";$installDir"
```

#### Option 2: System Settings (GUI)
1. Press `Win + X` and select "System"
2. Click "Advanced system settings"
3. Click "Environment Variables"
4. Under "User variables", select "Path" and click "Edit"
5. Click "New" and add: `C:\Users\<YourUsername>\.reshape\bin`
6. Click "OK" on all dialogs
7. Restart your terminal

### Linux / macOS

Add this line to your shell profile file:

```bash
export PATH="$PATH:$HOME/.local/bin"
```

#### For Bash
Add to `~/.bashrc` or `~/.bash_profile`:
```bash
echo 'export PATH="$PATH:$HOME/.local/bin"' >> ~/.bashrc
source ~/.bashrc
```

#### For Zsh (macOS default)
Add to `~/.zshrc`:
```bash
echo 'export PATH="$PATH:$HOME/.local/bin"' >> ~/.zshrc
source ~/.zshrc
```

#### For Fish
Add to `~/.config/fish/config.fish`:
```fish
set -gx PATH $PATH $HOME/.local/bin
```

## Verification

After adding to PATH and restarting your terminal, verify the installation:

```bash
reshape --version
```

You should see the installed version number.

## Troubleshooting

### "Command not found" or "not recognized"

1. **Restart your terminal** - PATH changes require a new terminal session
2. **Verify installation location** - Check that `reshape.exe` (Windows) or `reshape` (Linux/macOS) exists:
   - Windows: `C:\Users\<YourUsername>\.reshape\bin\reshape.exe`
   - Linux/macOS: `~/.local/bin/reshape`

3. **Check PATH** - Verify the directory is in your PATH:
   ```powershell
   # Windows PowerShell
   $env:PATH -split ';' | Select-String "reshape"
   ```
   ```bash
   # Linux/macOS
   echo $PATH | tr ':' '\n' | grep reshape
   ```

4. **Use full path temporarily** - While troubleshooting, you can run:
   ```powershell
   # Windows
   & "$env:USERPROFILE\.reshape\bin\reshape.exe" --help
   ```
   ```bash
   # Linux/macOS
   ~/.local/bin/reshape --help
   ```

### PATH Not Persisting

**Windows:** Make sure you're modifying the **User** environment variables, not just the session variable. System-level changes require administrator rights.

**Linux/macOS:** Ensure you're editing the correct shell profile file for your shell (`echo $SHELL` to check).

## Using Update Command

If you installed using the install script, you can update Reshape with:

```bash
reshape update
```

The update command will also verify and fix PATH configuration if needed.

## Alternative: Custom Installation Directory

You can install Reshape to a custom directory that's already in your PATH:

### Windows
```powershell
iex "& { $(irm https://raw.githubusercontent.com/jomaxso/Reshape/main/eng/scripts/install.ps1) } -InstallDir 'C:\MyTools'"
```

### Linux/macOS
```bash
curl -fsSL https://raw.githubusercontent.com/jomaxso/Reshape/main/eng/scripts/install.sh | bash -s -- --install-dir /usr/local/bin
```

Choose a directory that's already in your PATH (use `echo $PATH` to see your current PATH directories).
