using System;
using Gssoft.Gscad.ApplicationServices;
using Gssoft.Gscad.DatabaseServices;
using Gssoft.Gscad.EditorInput;
using Gssoft.Gscad.Geometry;

namespace WB_GCAD25
{
    public static class Active
    {
        public static Document Document => Application.DocumentManager.MdiActiveDocument;
        public static Editor Editor => Document.Editor;
        public static Database Database => Document.Database;

        public static void UsingTranscation(Action<Transaction> action)
        {
            using (var transaction = Database.TransactionManager.StartTransaction())
            {
                action(transaction);
                transaction.Commit();
            }
        }
    }
}