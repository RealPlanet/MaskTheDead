    using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using System.Timers;
using Unity.Netcode;
using UnityEngine;

namespace MaskTheDead.Components
{
    public class MaskTheDeadComponent : NetworkBehaviour
    {
        public bool PossessionChancePassed => Random.value < Plugin.TheConfiguration.PossesionChance;
        public bool CanRetryPossession => Plugin.TheConfiguration.RetryPossesionTime >= Plugin.TheConfiguration.RetryPossesionMinTime;

        private readonly Dictionary<GameObject, Coroutine> _NearbyBodies = new();
        private HauntedMaskItem _MaskComponent;
        private SphereCollider _Collider;

        private bool CanComponentUseRagdoll(RagdollGrabbableObject ragdoll)
        {
            if (ragdoll.isHeld || ragdoll.isHeldByEnemy)
            {
                Plugin.TheLogger.LogInfo("Cant attempt possession of held ragdoll...");
                return false;
            }

            if (ragdoll.isInShipRoom)
            {
                Plugin.TheLogger.LogInfo("Cant attempt possession of body in ship");
                return false;
            }

            return true;
        }

        #region Unity messages
        void Awake()
        {
            _MaskComponent = gameObject.GetComponent<HauntedMaskItem>();
            _Collider = gameObject.AddComponent<SphereCollider>();
            _Collider.radius = Plugin.TheConfiguration.MaskPossessionRange;
            _Collider.isTrigger = true;
        }

        void Update()
        {
            if(ShouldDispose())
            {
                Dispose();
                return;
            }

            // As soon as the mask is being held we stop attempting to possess dead bodies
            var maskIsHeld = _MaskComponent.isHeldByEnemy || _MaskComponent.isHeld;
            var maskIsInShip = _MaskComponent.isInShipRoom;
            var maskIsInElevator = _MaskComponent.isInElevator;

            if (maskIsHeld || maskIsInShip || maskIsInElevator)
            {
                _Collider.enabled = false;
                CleanupCoroutines();
                return;
            }

            _Collider.enabled = true;
        }

        public override void OnDestroy()
        {
            Plugin.TheLogger.LogInfo("Destroying repossession component!");
            CleanupCoroutines();
            base.OnDestroy();
        }

        public override void OnNetworkDespawn()
        {
            Plugin.TheLogger.LogInfo("Network despawn, cleaning repossession component!");
            base.OnNetworkDespawn();
            CleanupCoroutines();
        }

        void OnTriggerEnter(Collider other)
        {
            if(ShouldDispose())
            {
                Dispose();
                return;
            }

            RagdollGrabbableObject ragdoll = GetColliderRagdoll(other);
            if (ragdoll == null)
            {
                Plugin.TheLogger.LogInfo("Object in mask range is not ragdoll");
                return;
            }

            if (!CanComponentUseRagdoll(ragdoll))
                return;

            // This even could be fired twice for some reason, only do this once
            if(!_NearbyBodies.ContainsKey(ragdoll.gameObject))
            {
                _NearbyBodies.Add(ragdoll.gameObject, null);
                AttemptPossesion(ragdoll);
            }
        }

        void OnTriggerExit(Collider other)
        {
            Plugin.TheLogger.LogInfo($"Something is leaving the mask range, i have {_NearbyBodies.Count} body nearby!");

            RagdollGrabbableObject ragdoll = GetColliderRagdoll(other);
            if (ragdoll == null)
            {
                Plugin.TheLogger.LogInfo("The collider leaving is not of a ragdoll");
                return;
            }

            if(_NearbyBodies.TryGetValue(ragdoll.gameObject, out Coroutine coroutine))
            {
                if(coroutine != null)
                {
                    Plugin.TheLogger.LogInfo("Stopping coroutine for body leaving mask range");
                    StopCoroutine(coroutine);
                }

                _NearbyBodies.Remove(ragdoll.gameObject);
                return;
            }

            Plugin.TheLogger.LogWarning("Body leaving mask range was not in dicitonary of bodies!");
        }
        #endregion
        
        private RagdollGrabbableObject GetColliderRagdoll(Collider other)
        {
            var ragdoll = other.gameObject.GetComponent<RagdollGrabbableObject>();
            Plugin.TheLogger.LogInfo($"Collided with {other.gameObject}");
            if (ragdoll != null && ragdoll.bodyID.Value >= 0)
            {
                Plugin.TheLogger.LogInfo($"Found ragdoll, lucky!");
                return ragdoll;
            }

            // Not a ragdoll, try parent
            if(!other.name.Contains(".L") && !other.name.Contains(".R"))
            {
                Plugin.TheLogger.LogInfo($"This is not a bone!");
                return null;
            }

            Plugin.TheLogger.LogInfo($"Collided with a bone of a character, finding ragdoll on parents");
            return other.gameObject.GetComponentInParent<RagdollGrabbableObject>();
        }

        #region Coroutines
        // Start a coroutine for repossession attempt!
        private IEnumerator RepossessionCoroutine(object o)
        {
            yield return new WaitForSeconds(Plugin.TheConfiguration.RetryPossesionTime / 1000);
            yield return new WaitForEndOfFrame(); // Wait frame just in case this routine was stopped!

            RagdollGrabbableObject ragdoll = o as RagdollGrabbableObject;
            if(ragdoll != null && _NearbyBodies.TryGetValue(ragdoll.gameObject, out _))
            {
                Plugin.TheLogger.LogInfo("Ragdoll timer elapsed, attempting possession!");
                _NearbyBodies[ragdoll.gameObject] = null;
                AttemptPossesion(ragdoll);
            }
            else
            {
                Plugin.TheLogger.LogWarning("Ragdoll was deleted, stopping repossession retry!");
            }
        }

        void CleanupCoroutines()
        {
            if (_NearbyBodies.Count == 0)
                return;

            Plugin.TheLogger.LogInfo("Cleaning up all nearby bodies!");
            foreach (var kvp in _NearbyBodies)
            {
                if (kvp.Value != null)
                    StopCoroutine(kvp.Value);
            }

            _NearbyBodies.Clear();
        }
        #endregion

        #region Possession
        private void AttemptPossesion(RagdollGrabbableObject ragdoll)
        {
            if (!PossessionChancePassed)
            {
                Plugin.TheLogger.LogInfo("Chance failed, will not possess body");
                if (CanRetryPossession)
                {
                    Plugin.TheLogger.LogInfo("Starting coroutine for repossession");

                    // Stop the already existing co routine
                    if(_NearbyBodies[ragdoll.gameObject] != null)
                    {
                        StopCoroutine(_NearbyBodies[ragdoll.gameObject]);
                    }

                    Coroutine c = StartCoroutine(nameof(RepossessionCoroutine), ragdoll);
                    _NearbyBodies[ragdoll.gameObject] = c;
                }
                return;
            }

            Plugin.TheLogger.LogInfo("Chance for possession OK");
            BeginPossession(ragdoll);
        }

        private void BeginPossession(RagdollGrabbableObject ragdoll)
        {
            Plugin.TheLogger.LogInfo("!!POSSESSING DEAD BODY!!");
            var item = gameObject.GetComponent<HauntedMaskItem>();
            if (item == null)
            {
                Plugin.TheLogger.LogFatal("Could not find mask item component, aborting possession!");
                return;
            }

            Plugin.TheLogger.LogInfo("Spawning mimic");
            if(ragdoll.ragdoll.playerScript == null)
            {
                Plugin.TheLogger.LogFatal("Ragdoll's player script is null, aborting mimic spawn!");
                return;
            }

            //ragdoll.ragdoll.playerScript.deadBody = null;
            // Set the ownership of both to the server so we can do some RPC magic
            item.NetworkObject.RemoveOwnership();
            ragdoll.NetworkObject.RemoveOwnership();

            SetPreviouslyHeldByOf(item, ragdoll.ragdoll.playerScript);
            //item.playerHeldBy = null;
            item.CreateMimicServerRpc(ragdoll.isInFactory, ragdoll.ragdoll.transform.position);

            //// Despawn network objects!
            if(item != null)
            {
                item.NetworkObject.Despawn(); 
                Destroy(item);
                if(item.radarIcon != null && item.radarIcon.gameObject != null)
                {
                    Destroy(item.radarIcon.gameObject);
                }
            }

            if (ragdoll != null)
            {
                ragdoll.NetworkObject.Despawn();
                Destroy(ragdoll);
            }
        }

        private void SetPreviouslyHeldByOf(HauntedMaskItem item, PlayerControllerB c)
        {
            // Hacky way to ensure the mimic can safely spawn
            // otherwise we get a nre
            var prop = item.GetType().GetField("previousPlayerHeldBy",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            prop.SetValue(item, c);
            Plugin.TheLogger.LogInfo("Set private field previousPlayerHeldBy");
        }

        private bool HasFinishedAttaching(HauntedMaskItem item)
        {
            var prop = item.GetType().GetField("finishedAttaching",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            return (bool)prop.GetValue(item);
        }
        #endregion
    
        private bool ShouldDispose()
        {
            if (HasFinishedAttaching(_MaskComponent))
            {
                Plugin.TheLogger.LogInfo(">> MASK HAS ATTACHED ITSELF TO SOMEONE <<");
                Plugin.TheLogger.LogInfo(">> KILLING MASKTHEDEAD COMPONENT       <<");
                return true;
            }

            if (_MaskComponent == null || _Collider == null)
            {
                Plugin.TheLogger.LogWarning(">> MASK OR COLLIDER IS NULL <<");
                Plugin.TheLogger.LogInfo(">> KILLING MASKTHEDEAD COMPONENT       <<");

                return true;
            }

            return false;
        }

        private void Dispose()
        {
            //this.NetworkObject.Despawn();
            Plugin.TheLogger.LogInfo(">> DISPOSING <<");
            Destroy(this);
        }
    }
}
