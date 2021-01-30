using System;
using Core;

namespace App
{
    public class ApplicationHolder : Singleton<ApplicationHolder>
    {
        // Dependency Injection Container

        public int x = 0;

        public ApplicationHolder()
        {
            /*
             *
             */
        }

        // AfterSceneLoad
        // OnAfterSceneLoad

        /*
         * Add instance
         * Create instance
         * Get/Try get instance
         *
         */

        protected override void DoRelease()
        {
            throw new NotImplementedException();
        }
    }
}
