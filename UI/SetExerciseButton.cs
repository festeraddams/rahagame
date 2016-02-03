﻿namespace HSA.RehaGame.UI
{
    using DB;
    using Exercises;
    using Scene;
    using InGame;
    using UnityEngine;

    [RequireComponent(typeof(AudioSource))]

    public class SetExerciseButton : MonoBehaviour
    {
        private AudioSource audioSource;

        void Start()
        {
            var table = DBManager.Query("editor_exercise", "SELECT * FROM editor_exercise WHERE unityObjectName = '" + this.name + "';");

            audioSource = this.GetComponent<AudioSource>();
            audioSource.clip = table.GetResource<AudioClip>("auditiveName", "mp3");
        }

        public void LoadExrcise()
        {
            GameState.SetActiveExercise(this.name);
            LoadScene.LoadExercise();
        }
    }
}