using Uninvited.Editor;

GameplayVerification.Run();
if (System.Array.IndexOf(args, "--write-bootstrap-assets") >= 0) BootstrapAssets.Write();
