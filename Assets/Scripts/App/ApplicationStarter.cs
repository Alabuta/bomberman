using UnityEngine.Assertions;

namespace App
{
    public class ApplicationStarter
    {
        public ApplicationStarter()
        {
            ;
        }

        public void StartGame()
        {
            var applicationHolder = ApplicationHolder.Instance;
            Assert.IsNotNull(applicationHolder, "failed to initialize app holder");
        }
    }
}
