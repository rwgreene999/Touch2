using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace Touch2
{
    class Program
    {

        static void Main(string[] args)
        {
            Operations operations = new Operations();

            for (int idx = 0; idx < args.Length; idx++)
            {
                string entry = NormalizeArgument(args[idx], operations);
                ProcessArgument(operations, idx, entry);
                
            }
            ValidateOperations(args.Length, operations);

            if ( operations.Verbose)
            {
                Console.WriteLine(operations.ToString()); 
            }

            if (operations.ShowHelp)
            {
                ShowHelp(operations); 
            } else
            {
                PerformOperations(operations);
            }
            

            if ( operations.PauseBeforeEnding)
            {
                Console.ReadLine(); 
            }

        }



        private static void PerformOperations(Operations operations)
        {
            if (operations.DoFolderProcessing)
            {
                DateTime dtHighest = Fold(operations.StartingPoint, operations);
                UpdateFolderDateTime(operations.StartingPoint, operations, dtHighest);
                return;
            }

            Breakdown bd = BreakDownFilePath(operations.StartingPoint);
            if (!bd.hasWildcards)
            {
                UpdateFileDateTime(operations.StartingPoint, operations);
                return;
            }

            DoFilesWithWildCards(operations, bd);
            
        }

        private static void DoFilesWithWildCards(Operations operations, Breakdown bd)
        {
            var searchOptions = operations.DoSubFolderProcessing ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            string[] FilesToProcess = Directory.GetFiles(bd.path, bd.filter, SearchOption.AllDirectories);
            foreach (string filename in FilesToProcess)
            {
                UpdateFileDateTime(filename, operations);
            }
        }


        private static Breakdown BreakDownFilePath(string filename)
        {
            Breakdown bd = new Breakdown();
            bd.filename = filename = filename.Trim();
            int posLastWack = filename.LastIndexOf("\\");
            if (posLastWack < 0)
            {
                posLastWack = filename.LastIndexOf(":");
            }
            if (posLastWack < 0)
            {
                bd.hasPath = false;
                bd.path = "";
                bd.filter = filename;
            }
            else
            {
                bd.hasPath = true;
                bd.path = filename.Substring(0, posLastWack + 1);
                bd.hasPath = true;
                bd.filter = filename.Substring(posLastWack + 1);
            }
            if (bd.filter.IndexOfAny(new char[] { '*', '?' }) >= 0)
            {
                bd.hasWildcards = true;
            }
            if (bd.filter.Length > 0)
            {
                bd.hasFilter = true;
            }
            if (bd.path.EndsWith("\\"))
            {
                bd.path = bd.path = bd.path.Substring(0, bd.path.Length - 1);
            }
            return bd;
        }


        private class Breakdown
        {
            public string filename { get; set; }
            public string path { get; set; }
            public bool hasPath { get; set; }
            public string filter { get; set; }
            public bool hasFilter { get; set; }
            public bool hasWildcards { get; set; }
        }






        private static DateTime Fold(string Folder, Operations operations)
        {
            DateTime dtNewest = DateTime.MinValue;
            if (operations.DoSubFolderProcessing)
            {
                string[] subs = Directory.GetDirectories(Folder);
                foreach (string dir in subs)
                {
                    DateTime dtFromLower = Fold(dir, operations);
                    if (dtFromLower > DateTime.MinValue)
                    {
                        dtFromLower = dtFromLower.AddMinutes(operations.MinutesOffsetFromExistingTime);
                        UpdateFolderDateTime(dir, operations, dtFromLower);
                        dtNewest = dtNewest > dtFromLower ? dtNewest : dtFromLower;
                    }
                }
            }

            string[] files = Directory.GetFiles(Folder);
            foreach (var file in files)
            {
                DateTime dtThisFile = GetFileTime(file, operations.WhichDateToModify);
                dtNewest = dtNewest > dtThisFile ? dtNewest : dtThisFile;
            }

            return dtNewest;
        }



        private static void UpdateFileDateTime(string filename, Operations operations)
        {
            DateTime dt = operations.UseThisDate;
            if (operations.MinutesOffsetFromExistingTime != 0)
            {
                dt = GetFileTime(filename, operations.WhichDateToModify);
                dt = dt.AddMinutes(operations.MinutesOffsetFromExistingTime);
            }
            UpdateFileDateTime(filename, operations, dt);
        }



        private static DateTime GetFileTime(string p, DateToModify dateToModify)
        {
            switch (dateToModify)
            {
                case DateToModify.Updated:
                    return File.GetLastWriteTime(p);
                    break;
                case DateToModify.Accessed:
                    return File.GetLastAccessTime(p);
                    break;
                case DateToModify.Created:
                    return File.GetCreationTime(p);
                    break;
            }
            throw new Exception("Invalid condition on GetFileTime");
        }





        private static void UpdateFileDateTime(string Filename, Operations operations, DateTime dt)
        {
            try
            {
                if (!operations.ListOnly)
                {
                    switch (operations.WhichDateToModify)
                    {
                        case DateToModify.Updated:
                            File.SetLastWriteTime(Filename, dt);
                            break;
                        case DateToModify.Accessed:
                            File.SetLastAccessTime(Filename, dt);
                            break;
                        case DateToModify.Created:
                            File.SetCreationTime(Filename, dt);
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error updating datetime on file: " + Filename);
                Console.WriteLine(ex.Message); 
            }
            if (operations.Verbose)
            {
                Console.WriteLine(Filename + " updated to " + dt.ToString());
            }
        }




        private static void UpdateFolderDateTime(string Foldername, Operations operations, DateTime dt)
        {
            if (!operations.ListOnly)
            {
                try
                {
                    switch (operations.WhichDateToModify)
                    {
                        case DateToModify.Updated:
                            Directory.SetLastWriteTime(Foldername, dt);
                            break;
                        case DateToModify.Accessed:
                            Directory.SetLastAccessTime(Foldername, dt);
                            break;
                        case DateToModify.Created:
                            Directory.SetCreationTime(Foldername, dt);
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error updating datetime on folder: " + Foldername);
                    Console.WriteLine(ex.Message); 
                }
            }
            if (operations.Verbose)
            {
                Console.WriteLine(Foldername + " updated to " + dt.ToString());
            }
        }



        private static void ValidateOperations(int CntArguments, Operations operations)
        {
            if (CntArguments < 1 )
            {
                operations.ShowHelp = true;
            }

            if (operations.MinutesOffsetFromExistingTime == 0 && operations.UseThisDate == DateTime.MinValue)
            {
                operations.UseThisDate = DateTime.Now;
            }
        }

        private static bool ProcessArgument(Operations operations, int idx, string entry)
        {
            if (idx == 0)
            {
                operations.StartingPoint = entry;
                operations.DoFolderProcessing = ThisIsFolder(operations.StartingPoint);

            }
            else if (entry == "dateaccessed")
            {
                operations.WhichDateToModify = DateToModify.Accessed;

            }
            else if (entry == "datecreated")
            {
                operations.WhichDateToModify = DateToModify.Created;

            }
            else if (entry.StartsWith("+") || entry.StartsWith("-"))
            {
                int min;
                int.TryParse(entry, out min);
                operations.MinutesOffsetFromExistingTime = min;

            }
            else if (entry.StartsWith( "s"))
            {
                operations.DoSubFolderProcessing = true;
            }

            else if (entry.StartsWith("l"))
            {
                operations.ListOnly = true;
                operations.Verbose = true;
            }

            else if (entry.StartsWith("v"))
            {
                operations.Verbose = true;
            }

            else if (entry.StartsWith("p"))
            {
                operations.PauseBeforeEnding= true;
            }

            else if (entry.StartsWith("?"))
            {
                operations.ShowHelp = true; 
            }

            else if (entry.Length >= 6)
            {
                if ( ThisIsNumeric(entry.Substring(0,4)) )
                {
                    DateTime dt;
                    if ( !DateTime.TryParse(entry, out dt) )
                    {
                        operations.message = "Invalid date of: " + entry;
                        operations.ShowHelp = true; 
                    }
                    operations.UseThisDate = dt;
                }
            }
            return operations.ShowHelp;
        }

        private static string NormalizeArgument(string arg, Operations operations)
        {
            string entry = arg.ToLower();
            if (entry == "?" || entry == "/?" || entry == "-?")
            {
                operations.ShowHelp = true;
            }
            else if (entry.StartsWith("-"))
            {
                // change all '-' to '/' unless it is a -minutes (no leading code ) 
                if (entry.Length < 7 && entry.Length > 1 && ThisIsNumeric(entry.Substring(1)))
                {
                    // leave it, it is -minutes 
                }
                else
                {
                    entry = entry.Substring(1);
                }
            }
            else if (entry.StartsWith("/"))
            {
                entry = entry.Substring(1);
            }
            return entry;
        }




        public static bool ThisIsNumeric(string x)
        {
            int test1;
            if (int.TryParse(x, out test1))
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        public static bool ThisIsFolder(string name)
        {
            if (Directory.Exists(name))
            {
                return true;
            }
            return false;
        }

        public static void ShowHelp(Operations operations)
        {
            Console.WriteLine(
@"
Touch2  -- to change the datetime stamp of file, files or folder

Examples:
touch2 c:\foldername\file.txt [options]
touch2 c:\foldername\file.* [options]
touch2 c:\foldername\file?.* [options]
touch2 c:\foldername [options]

File datetimes are changed to current time or as specified in options
Folder datetimes are changed to newest file in folder or as specified

date Options:
  /+99 or /-99 or /yyyy-mm-ddThh:mm:ss
  + or - numbers add/subtract those minutes from current datetime
  yyyy-mm-ddThh:mm:ss date applied to files and folders,
        Mininum valid date is year and month (ex: yyyy-mm )
other options:
  /s            traverse subfolders
  /v            verbose, list the files and actions taken
  /l            List actions, but change nothing
  /p            Pause at the end of the list
"
                ); 
            
        }



    }


    internal enum DateToModify { Updated = 0, Accessed, Created }
    internal class Operations
    {
        public bool PauseBeforeEnding { get; set; }
        public bool DoFolderProcessing { get; set; }
        public bool Verbose { get; set; }
        public bool ListOnly { get; set; }
        public bool DoSubFolderProcessing { get; set; }
        public int MinutesOffsetFromExistingTime { get; set; }
        public DateTime UseThisDate { get; set; }
        public string StartingPoint { get; set; }
        public int CntFilesChanges { get; set; }
        public DateToModify WhichDateToModify { get; set; }
        public bool ShowHelp { get; set; }
        public string message { get; set; }

        public string ToString()
        {
            return String.Format(@"
DoFolderProcessing:{0}
Verbose:{1}
ListOnly:{2}
DoSubFolderProcessing:{3}
MinutesOffsetFromExistingTime:{4}
UseThisDate:{5}
StartingPoint:{6}
CntFilesChanges:{7}
WhichDateToModify:{8}
ShowHelp:{9}
message:{10}
", DoFolderProcessing
    , Verbose
    , ListOnly
    , DoSubFolderProcessing
    , MinutesOffsetFromExistingTime
    , UseThisDate
    , StartingPoint
    , CntFilesChanges
    , WhichDateToModify
    , ShowHelp
    , message);
        }
    }


}
