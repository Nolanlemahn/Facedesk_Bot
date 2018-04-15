using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FaceDesk_Bot.Permissions
{
  class SimplePermissions
  {
    public static List<ulong> Owners = new List<ulong>();

    //In owners.txt, newline-separate UIDs that should also act as the owner
    //see LocalExtensions.IsOwner
    public static void LoadOwners()
    {
      string[] owners =
        System.IO.File.ReadAllText(Path.Combine(EntryPoint.RunningFolder, "owners.txt")).Split('\n');
      foreach (string owner in owners)
      {
        ulong uid = Convert.ToUInt64(owner);
        Owners.Add(uid);
      }
    }
  }
}
