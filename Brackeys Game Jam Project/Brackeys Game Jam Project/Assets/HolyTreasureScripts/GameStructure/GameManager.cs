﻿using System;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.Characters.ThirdPerson;
using Assets.HolyTreasureScripts.UI;
using Assets.HolyTreasureScripts.Audio;

namespace Assets.HolyTreasureScripts.GameStructure {
    public class GameManager : MonoBehaviour {

        #region Variables
        /// <summary>
        /// The static instance of this class.
        /// </summary>
        public static GameManager Instance;

        /// <summary>
        /// Return true if walls are reinforced, or false if not.
        /// </summary>
        public bool reinforced { get; set; }
        /// <summary>
        /// How many mines have been exploded thus far.
        /// </summary>
        public int minesExploded { get; private set; }

        /// <summary>
        /// The transform that holds all the mine icons.
        /// </summary>
        public Transform mineIconParent;
        /// <summary>
        /// The transform that holds all the hit icons.
        /// </summary>
        public Transform hitIconParent;
        /// <summary>
        /// The ground floors in the game.
        /// </summary>
        public GameObject[] groundFloors;
        /// <summary>
        /// The Big Light that can illuminate the entire cave.
        /// </summary>
        public Light bigLight;
        /// <summary>
        /// The Text Component that has the Ready...Go text on it.
        /// </summary>
        public Text readyGo;

        /// <summary>
        /// The Gameplay UI class in the scene.
        /// </summary>
        private GameplayUI gameUI;
        /// <summary>
        /// The User Controller within the scene.
        /// </summary>
        private ThirdPersonUserControl useCon;
        /// <summary>
        /// The Scene Transitioner in the scene.
        /// </summary>
        private SceneTransitioner sceneTran;
        /// <summary>
        /// The Audio Manager in the scene.
        /// </summary>
        private AudioManager audioMan;
        /// <summary>
        /// The audio source that holds the lava sound.
        /// </summary>
        private AudioSource lavaSource;
        /// <summary>
        /// The Shop class in the scene.
        /// </summary>
        private Shop shop;
        /// <summary>
        /// A collection of all the mine icons.
        /// </summary>
        private Image[] mineIcons;
        /// <summary>
        /// A collection of all the hit icons.
        /// </summary>
        private Image[] hitIcons;
        /// <summary>
        /// Return true if class should check for a game over, or false if not.
        /// </summary>
        private bool checkForGameOver = true;
        /// <summary>
        /// Return true if the music has already been played, or false if not.
        /// </summary>
        private bool musicAlreadyPlayed = false;
        /// <summary>
        /// The current floor the player is on.
        /// </summary>
        private int currentFloor = 0;
        #endregion

        private void Awake() {
            if (Instance == null) {
                Instance = this;
            } else {
                Debug.LogError("There is more than one instance of the Game Manager class in the scene!");
            }
        }

        private void Start() {
            mineIcons = mineIconParent.GetComponentsInChildren<Image>();
            hitIcons = hitIconParent.GetComponentsInChildren<Image>();
            foreach (Image mine in mineIcons) {
                mine.enabled = false;
            }
            foreach (Image hit in hitIcons) {
                hit.enabled = false;
            }
            UpdateMineStatus(0);

            gameUI = GameplayUI.Instance;
            useCon = GameObject.FindGameObjectWithTag("Player").GetComponent<ThirdPersonUserControl>();
            sceneTran = SceneTransitioner.Instance;
            shop = Shop.Instance;
            audioMan = AudioManager.Instance;
            audioMan.PlaySound("Floor1");
            lavaSource = audioMan.PlaySound("Lava");
        }

        /// <summary>
        /// Updates the game's mine status.
        /// </summary>
        /// <param name="hits">
        /// The amount of mines that have been hit.
        /// </param>
        public void UpdateMineStatus(int hits) {
            if (hits > 5) {
                Debug.LogError("Too many hits being reported!");
                return;
            }

            if (reinforced) {
                foreach (Image mine in mineIcons) {
                    mine.enabled = true;
                }
            } else {
                if (hits > 3) {
                    Debug.LogError("Walls aren't being reinforced, too many hits being reported.");
                    return;
                }
                for (int i = 4; i > 1; i--) {
                    mineIcons[i].enabled = true;
                }
            }

            int length = 4 - hits;

            for (int i = 4; i > length; i--) {
                hitIcons[i].enabled = true;
            }

            minesExploded = hits;

            if (reinforced) {
                if (minesExploded == 5) {
                    ResetMineStatusForNewFloor();
                }
            } else {
                if (minesExploded == 3) {
                    ResetMineStatusForNewFloor();
                }
            }
        }

        private void ResetMineStatusForNewFloor() {
            Debug.Log("Next Floor");

            if (bigLight != null) {
                bigLight.enabled = false; 
            }
            audioMan.PlaySound("Break");
            if (!musicAlreadyPlayed) {
                audioMan.StopSound("Floor1");
                audioMan.PlaySound("Floor2");
                musicAlreadyPlayed = true;
            }
            groundFloors[currentFloor].SetActive(false);
            currentFloor++;

            switch (currentFloor) {
                case 0:
                    lavaSource.volume = 0.02f;
                    break;
                case 1:
                    lavaSource.volume = 0.04f;
                    break;
                case 2:
                    lavaSource.volume = 0.08f;
                    break;
                case 3:
                    lavaSource.volume = 0.12f;
                    break;
                default:
                    lavaSource.volume = 0.02f;
                    break;
            }

            gameUI.UpdateFloorText(currentFloor + 1);
            gameUI.oxygenDecayRate *= 2;

            if (bigLight != null) {
                shop.price_light = shop.baseLightPrice * (currentFloor + 1);
                shop.UpdatePriceText(shop.text_light, shop.price_light);
                shop.lightItemGroup.ChangeDisplay("Shine Light", "BUY"); 
            }

            if (bigLight != null) {
                shop.price_floor = shop.baseWallPrice * (currentFloor + 1);
                shop.UpdatePriceText(shop.text_floor, shop.price_floor);
                shop.floorItemGroup.ChangeDisplay("Reinforce Floor", "BUY"); 
            }


            minesExploded = 0;
            reinforced = false;
            foreach (Image mine in mineIcons) {
                mine.enabled = false;
            }
            foreach (Image hit in hitIcons) {
                hit.enabled = false;
            }
            UpdateMineStatus(0);
        }

        private void Update() {
            if (checkForGameOver) {
                if (gameUI.oxygenValue <= 0) {
                    useCon.ableToMove = false;
                    readyGo.text = "GAME OVER";
                    StartCoroutine(FadeOuttaHere());
                    Debug.Log("Game Over");
                    checkForGameOver = false;
                }
            }
        }

        IEnumerator FadeOuttaHere() {
            yield return new WaitForSeconds(3);
            sceneTran.attachedAnimator.SetTrigger("FadeOut");
            StopCoroutine(FadeOuttaHere());
        }
    }
}