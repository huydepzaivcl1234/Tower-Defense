#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Incremental Git push helper for this Unity project.
///
/// Security rules:
/// - Only stages Assets, Packages, ProjectSettings, Tools and .gitignore.
/// - Requires the local branch to be main.
/// - Requires origin to point to the expected Tower-Defense repository.
/// - Blocks common secret/private-key filenames.
/// - Scans staged text files for high-confidence credential patterns before commit/push.
/// - Never stores or prints GitHub credentials/tokens.
///
/// Git itself handles authentication using the user's existing credential manager / SSH setup.
/// </summary>
public static class SecureQuickPushTool
{
    private const string ExpectedRepository = "huydepzaivcl1234/Tower-Defense";

    private static readonly string[] AllowedRoots =
    {
        "Assets",
        "Packages",
        "ProjectSettings",
        "Tools",
        ".gitignore"
    };

    private static readonly string[] BlockedFileNameFragments =
    {
        ".env",
        "id_rsa",
        "id_ed25519",
        "private_key",
        "private-key",
        "service-account",
        "service_account",
        "credentials.json",
        "client_secret",
        "client-secret"
    };

    private static readonly HashSet<string> BlockedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".pem", ".pfx", ".p12", ".key", ".keystore", ".jks"
    };

    // These are intentionally high-confidence patterns to avoid blocking ordinary source code
    // that merely mentions words such as API_KEY or TOKEN.
    private static readonly Regex[] SecretPatterns =
    {
        new Regex(@"-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----", RegexOptions.Compiled),
        new Regex(@"\bgh[pousr]_[A-Za-z0-9_]{30,}\b", RegexOptions.Compiled),
        new Regex(@"\bgithub_pat_[A-Za-z0-9_]{40,}\b", RegexOptions.Compiled),
        new Regex(@"\bsk-[A-Za-z0-9_-]{20,}\b", RegexOptions.Compiled),
        new Regex(@"\bAKIA[0-9A-Z]{16}\b", RegexOptions.Compiled),
        new Regex("(?i)\\b(?:api[_-]?key|secret|access[_-]?token|auth[_-]?token|password)\\b\\s*[:=]\\s*[\"'](?!YOUR_|PLACEHOLDER|EXAMPLE|CHANGE_ME|REPLACE_ME)[^\"'\\r\\n]{12,}[\"']", RegexOptions.Compiled)
    };

    private static readonly HashSet<string> TextExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".shader", ".hlsl", ".cginc", ".shadergraph", ".json", ".txt", ".xml",
        ".yaml", ".yml", ".asset", ".prefab", ".unity", ".mat", ".meta", ".asmdef",
        ".asmref", ".uxml", ".uss", ".md", ".ps1", ".cmd", ".bat", ".gitignore"
    };

    [MenuItem("Tower Defense/Git/Quick Push", priority = 2000)]
    public static void QuickPush()
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot) || !Directory.Exists(Path.Combine(projectRoot, ".git")))
        {
            EditorUtility.DisplayDialog("Quick Push", "Không tìm thấy Git repository ở thư mục project.", "OK");
            return;
        }

        try
        {
            EditorUtility.DisplayProgressBar("Secure Quick Push", "Đang kiểm tra Git...", 0.08f);

            string branch = RunGit(projectRoot, "branch --show-current").Trim();
            if (!string.Equals(branch, "main", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Đang ở branch '{branch}', không phải 'main'. Quick Push bị chặn để tránh push nhầm branch.");
            }

            string remote = RunGit(projectRoot, "remote get-url origin").Trim();
            string normalizedRemote = remote.Replace('\\', '/');
            if (normalizedRemote.IndexOf(ExpectedRepository, StringComparison.OrdinalIgnoreCase) < 0)
            {
                throw new InvalidOperationException(
                    "Remote origin không phải repository Tower-Defense đã khóa trong tool.\n\n" + remote);
            }

            EditorUtility.DisplayProgressBar("Secure Quick Push", "Đang stage các file Unity đã thay đổi...", 0.22f);

            // '--' prevents path names from being parsed as Git options.
            string roots = string.Join(" ", AllowedRoots.Select(QuoteGitArg));
            RunGit(projectRoot, "add -- " + roots);

            string stagedOutput = RunGit(projectRoot, "diff --cached --name-only --diff-filter=ACMRD");
            List<string> stagedFiles = stagedOutput
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToList();

            if (stagedFiles.Count == 0)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Quick Push", "Không có thay đổi mới để push.", "OK");
                return;
            }

            EditorUtility.DisplayProgressBar("Secure Quick Push", "Đang kiểm tra file nhạy cảm...", 0.42f);
            List<string> securityProblems = ScanForSecrets(projectRoot, stagedFiles);
            if (securityProblems.Count > 0)
            {
                // Unstage only; never delete/modify the user's local files.
                RunGit(projectRoot, "reset -- " + roots);

                string details = string.Join("\n", securityProblems.Take(12));
                if (securityProblems.Count > 12)
                    details += $"\n... và {securityProblems.Count - 12} mục khác";

                throw new InvalidOperationException(
                    "ĐÃ CHẶN PUSH vì phát hiện file/nội dung có khả năng chứa thông tin nhạy cảm:\n\n" + details +
                    "\n\nKhông file local nào bị xóa. Các thay đổi vừa stage đã được unstage.");
            }

            EditorUtility.DisplayProgressBar("Secure Quick Push", $"Đang commit {stagedFiles.Count} file thay đổi...", 0.62f);

            string commitMessage = "Unity quick update " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            RunGit(projectRoot, "commit -m " + QuoteGitArg(commitMessage));

            EditorUtility.DisplayProgressBar("Secure Quick Push", "Đang push lên origin/main...", 0.82f);
            string pushOutput = RunGit(projectRoot, "push origin main");

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Quick Push thành công",
                $"Đã push {stagedFiles.Count} file thay đổi lên main.\n\n" +
                "Tool không lưu GitHub password/token và đã chạy kiểm tra secret trước khi push.",
                "OK");

            UnityEngine.Debug.Log("[SecureQuickPush] Push thành công.\n" + SanitizeGitOutput(pushOutput));
        }
        catch (Exception ex)
        {
            EditorUtility.ClearProgressBar();
            UnityEngine.Debug.LogError("[SecureQuickPush] " + ex.Message);
            EditorUtility.DisplayDialog("Quick Push bị dừng", ex.Message, "OK");
        }
    }

    private static List<string> ScanForSecrets(string projectRoot, List<string> stagedFiles)
    {
        var problems = new List<string>();

        foreach (string relativePath in stagedFiles)
        {
            string normalized = relativePath.Replace('\\', '/');
            string fileName = Path.GetFileName(normalized);
            string lowerName = fileName.ToLowerInvariant();
            string extension = Path.GetExtension(fileName);

            if (BlockedFileNameFragments.Any(fragment =>
                    lowerName.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                problems.Add("Tên file nhạy cảm: " + normalized);
                continue;
            }

            if (BlockedExtensions.Contains(extension))
            {
                problems.Add("Định dạng khóa/chứng thư nhạy cảm: " + normalized);
                continue;
            }

            string fullPath = Path.GetFullPath(Path.Combine(projectRoot, relativePath));
            if (!fullPath.StartsWith(Path.GetFullPath(projectRoot), StringComparison.OrdinalIgnoreCase))
            {
                problems.Add("Đường dẫn bất thường: " + normalized);
                continue;
            }

            // Deleted files do not exist in the working tree and cannot introduce a new secret.
            if (!File.Exists(fullPath) || !ShouldScanText(fileName))
                continue;

            var info = new FileInfo(fullPath);
            if (info.Length > 5 * 1024 * 1024)
                continue; // Avoid loading huge text assets; filename checks still apply.

            string text;
            try
            {
                text = File.ReadAllText(fullPath, Encoding.UTF8);
            }
            catch
            {
                continue; // Binary/non-UTF8 content is not interpreted as credentials here.
            }

            foreach (Regex pattern in SecretPatterns)
            {
                if (pattern.IsMatch(text))
                {
                    problems.Add("Nội dung có dấu hiệu credential/secret: " + normalized);
                    break;
                }
            }
        }

        return problems;
    }

    private static bool ShouldScanText(string fileName)
    {
        if (string.Equals(fileName, ".gitignore", StringComparison.OrdinalIgnoreCase))
            return true;
        return TextExtensions.Contains(Path.GetExtension(fileName));
    }

    private static string RunGit(string workingDirectory, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using (Process process = Process.Start(startInfo))
        {
            if (process == null)
                throw new InvalidOperationException("Không khởi chạy được Git. Hãy kiểm tra Git đã được cài và có trong PATH.");

            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                string safeError = SanitizeGitOutput(string.IsNullOrWhiteSpace(stderr) ? stdout : stderr);
                throw new InvalidOperationException($"Git command thất bại (exit {process.ExitCode}).\n\n{safeError}");
            }

            return string.IsNullOrWhiteSpace(stdout) ? stderr : stdout;
        }
    }

    private static string QuoteGitArg(string value)
    {
        if (value == null) return "\"\"";
        return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    private static string SanitizeGitOutput(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        // Last-resort redaction in case a credential ever appears in Git output.
        text = Regex.Replace(text, @"(?i)(https?://)[^/@\s]+@", "$1[REDACTED]@");
        text = Regex.Replace(text, @"\bgh[pousr]_[A-Za-z0-9_]{20,}\b", "[REDACTED_GITHUB_TOKEN]");
        text = Regex.Replace(text, @"\bgithub_pat_[A-Za-z0-9_]{20,}\b", "[REDACTED_GITHUB_TOKEN]");
        text = Regex.Replace(text, @"\bsk-[A-Za-z0-9_-]{16,}\b", "[REDACTED_API_KEY]");
        return text.Trim();
    }
}
#endif