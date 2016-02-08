﻿namespace HSA.RehaGame.Scene
{
    using System.Collections.Generic;
    using System.Linq;
    using InGame;
    using Logging;
    using UnityEngine;
    using USM = UnityEngine.SceneManagement;

    public class SceneManager : MonoBehaviour
    {
        private static Logger<SceneManager> logger = new Logger<SceneManager>();

        private static GameObject menuItems;
        private static List<string> previousScenes = new List<string>();

        private int sceneCount;
        private USM.Scene[] scenes;

        // Use this for initialization
        void Start()
        {
            logger.AddLogAppender<ConsoleAppender>();

            sceneCount = USM.SceneManager.sceneCount;
            scenes = new USM.Scene[sceneCount];

            for(int i = 0; i < sceneCount; i++)
            {
                scenes[i] = USM.SceneManager.GetSceneAt(i);
            }
        }

        private void LoadNewScene(string scene, bool addPrevious = true)
        {
            if (addPrevious)
                previousScenes.Add(USM.SceneManager.GetActiveScene().name);

            USM.SceneManager.LoadScene(scene);
        }

        public void LoadExercise()
        {
            if (GameState.ActiveExercise != null)
                this.LoadNewScene(GameState.ActiveExercise.UnityObjectName);
        }

        public void ReloadSettings()
        {
            this.LoadNewScene("Settings", false);
        }

        public void LoadNewUser()
        {
            this.LoadNewScene("NewUser");
        }
        public void LoadStatistics()
        {
            this.LoadNewScene("Statistics");
        }

        public void ReturnToWindows()
        {
            Application.Quit();
        }

        public void GoOneSceneBack()
        {
            var last = previousScenes.Last();
            previousScenes.Remove(last);

            this.LoadNewScene(last, false);
        }

        public void LoadTrainingMode()
        {
            this.LoadNewScene("TrainingMode");
        }

        public void LoadMainMenu()
        {
            this.LoadNewScene("MainMenu");
        }

        public void LoadUsersSlection()
        {
            this.LoadNewScene("UserSelection");
        }

        public void PreviousScene()
        {
            GoOneSceneBack();
        }
    }
}
