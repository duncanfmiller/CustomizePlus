﻿// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Numerics;
using CustomizePlus.Data;
using CustomizePlus.Data.Profile;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using ImGuiNET;
using Newtonsoft.Json;

namespace CustomizePlus.UI.Windows.Debug
{
    public class IPCTestWindow : WindowBase
    {
        private readonly List<string> _boneCodenames = BoneData.GetBoneCodenames();
        private readonly List<string> _boneCodenamesUsed = new();
        private readonly List<string> _boneDispNames = BoneData.GetBoneDisplayNames();
        private readonly List<string> _boneDispNamesUsed = new();
        private readonly Dictionary<string, BoneTransform> _boneValuesNew = new();

        private readonly Dictionary<string, BoneTransform> _boneValuesOriginal = new();

        private bool _automaticBoneAttribute;

        private BoneAttribute _boneAttribute;

        private ICallGateSubscriber<string, string>? _getCharacterProfile;
        private string _newScaleCharacter = string.Empty;

        private string _newScaleName = string.Empty;
        private string _originalScaleCharacter = string.Empty;
        private string _originalScaleName = string.Empty;

        private bool _reset;

        //private readonly ICallGateSubscriber<string, Character?, object>? ProviderSetCharacterProfileToCharacter;
        private ICallGateSubscriber<string, object>? _revert;
        private BoneTransform _rootEditsContainer = new();

        private bool _scaleEnabled;

        //private readonly ICallGateSubscriber<Character?, string?>? ProviderGetCharacterProfileFromCharacter;
        private ICallGateSubscriber<string, string, object>? _setCharacterProfile;

        //private DalamudPluginInterface localPlugin;
        private bool _subscribed = true;
        //private readonly ICallGateSubscriber<Character?, object>? ProviderRevertCharacter;
        //private readonly ICallGateSubscriber<string>? _getApiVersion;
        //private readonly ICallGateSubscriber<string?, object?>? _onScaleUpdate;

        protected CharacterProfile? Scale { get; private set; }

        protected override string Title => $"(WIP) IPC Test: {_newScaleCharacter}";
        protected CharacterProfile? ScaleUpdated { get; private set; }

        private void SubscribeEvents()
        {
            if (!_subscribed)
            {
                _subscribed = true;
            }
        }

        public void UnsubscribeEvents()
        {
            if (_subscribed)
            {
                _subscribed = false;
            }
        }

        public override void Dispose()
        {
            _subscribed = false;
        }

        public static void Show(DalamudPluginInterface pi)
        {
            var localPlugin = pi;
            var editWnd = Plugin.InterfaceManager.Show<IPCTestWindow>();
            editWnd._getCharacterProfile =
                localPlugin.GetIpcSubscriber<string, string>("CustomizePlus.GetCharacterProfile");
            //localPlugin.GetIpcSubscriber<Character?, string?> ProviderGetCharacterProfileFromCharacter;
            editWnd._setCharacterProfile =
                localPlugin.GetIpcSubscriber<string, string, object>("CustomizePlus.SetCharacterProfile");
            //localPlugin.GetIpcSubscriber<string, Character?, object> ProviderSetCharacterProfileToCharacter;
            editWnd._revert = localPlugin.GetIpcSubscriber<string, object>("CustomizePlus.Revert");
            //localPlugin.GetIpcSubscriber<Character?, object>? ProviderRevertCharacter;
            //_getApiVersion = localPlugin.GetIpcSubscriber<string>("CustomizePlus.GetApiVersion");
            //_onScaleUpdate = localPlugin.GetIpcSubscriber<string?, object?>("CustomizePlus.OnScaleUpdate"); ;
            //UnsubscribeEvents();


            var prof = new CharacterProfile();
            editWnd.Scale = prof;
            editWnd.ScaleUpdated = prof;
            if (prof == null)
            {
            }

            editWnd.ScaleUpdated = prof;
            editWnd._originalScaleName = prof.ProfileName;
            editWnd._originalScaleCharacter = prof.CharacterName;
            editWnd._newScaleCharacter = prof.CharacterName;

            editWnd._scaleEnabled = prof.Enabled;

            for (var i = 0; i < editWnd._boneCodenames.Count && i < editWnd._boneDispNames.Count; i++)
            {
                BoneTransform tempContainer = new() { Scaling = Vector3.One };
                if (prof.Bones.TryGetValue(editWnd._boneCodenames[i], out tempContainer))
                {
                    editWnd._boneValuesOriginal.Add(editWnd._boneCodenames[i], tempContainer);
                    editWnd._boneValuesNew.Add(editWnd._boneCodenames[i], tempContainer);
                    editWnd._boneDispNamesUsed.Add(editWnd._boneDispNames[i]);
                    editWnd._boneCodenamesUsed.Add(editWnd._boneCodenames[i]);
                }
            }

            editWnd._originalScaleName = prof.ProfileName;
            editWnd._originalScaleCharacter = prof.CharacterName;
            editWnd._newScaleName = editWnd._originalScaleName;
            editWnd._newScaleCharacter = editWnd._originalScaleCharacter;


            //DrawContents();
        }

        protected override void DrawContents()
        {
            try
            {
                SubscribeEvents();
                DrawScaleEdit(new CharacterProfile(), DalamudServices.PluginInterface);
            }
            catch (Exception e)
            {
                PluginLog.LogError($"Error during IPC Tests:\n{e}");
            }
        }

        public void DrawScaleEdit(CharacterProfile scale, DalamudPluginInterface pi)
        {
            var newScaleNameTemp = _newScaleName;
            var newScaleCharacterTemp = _newScaleCharacter;
            var enabledTemp = _scaleEnabled;
            var resetTemp = _reset;

            if (ImGui.Checkbox("Enable", ref enabledTemp))
            {
                _scaleEnabled = enabledTemp;
                if (_automaticBoneAttribute)
                {
                }
            }

            ImGui.SameLine();

            ImGui.SetNextItemWidth(200);

            if (ImGui.InputText("Character Name", ref newScaleCharacterTemp, 1024))
            {
                _newScaleCharacter = newScaleCharacterTemp;
            }

            ImGui.SameLine();

            ImGui.SetNextItemWidth(300);
            if (ImGui.InputText("Scale Name", ref newScaleNameTemp, 1024))
            {
                _newScaleName = newScaleNameTemp;
            }

            ImGui.SameLine();

            var autoModeEnable = _automaticBoneAttribute;
            if (ImGui.Checkbox("Automatic Mode", ref autoModeEnable))
            {
                _automaticBoneAttribute = autoModeEnable;
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Applies changes automatically without saving.");
            }

            if (ImGui.RadioButton("Position", _boneAttribute == BoneAttribute.Position))
            {
                _boneAttribute = BoneAttribute.Position;
            }

            ImGui.SameLine();
            if (ImGui.RadioButton("Rotation", _boneAttribute == BoneAttribute.Rotation))
            {
                _boneAttribute = BoneAttribute.Rotation;
            }

            ImGui.SameLine();
            if (ImGui.RadioButton("Scale", _boneAttribute == BoneAttribute.Scale))
            {
                _boneAttribute = BoneAttribute.Scale;
            }

            ImGui.Separator();

            if (ImGuiComponents.IconButton(-1, FontAwesomeIcon.Recycle))
            {
                _rootEditsContainer = new BoneTransform();
                if (_automaticBoneAttribute)
                {
                    UpdateCurrent("n_root", _rootEditsContainer);
                }

                _reset = true;
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Reset");
            }

            ImGui.SameLine();

            var rootLocalTemp = Vector3.One;
            var isRootControlDisabled = false;
            switch (_boneAttribute)
            {
                case BoneAttribute.Position:
                    rootLocalTemp = _rootEditsContainer.Translation;
                    break;
                case BoneAttribute.Rotation:
                    rootLocalTemp = Vector3.Zero;
                    isRootControlDisabled = true;
                    break;
                case BoneAttribute.Scale:
                    rootLocalTemp = _rootEditsContainer.Scaling;
                    break;
            }

            if (isRootControlDisabled)
            {
                ImGui.BeginDisabled();
            }

            if (ImGui.DragFloat3("Root", ref rootLocalTemp, 0.001f, 0f, 10f))
            {
                if (_reset)
                {
                    rootLocalTemp = new Vector3(1f, 1f, 1f);
                    _reset = false;
                }
                /*else if (!((rootLocalTemp.X == rootLocalTemp.Y) && (rootLocalTemp.X == rootLocalTemp.Z) && (rootLocalTemp.Y == rootLocalTemp.Z)))
                {
                    rootLocalTemp.W = 0;
                }
                else if (rootLocalTemp.W != 0)
                {
                    rootLocalTemp.X = rootLocalTemp.W;
                    rootLocalTemp.Y = rootLocalTemp.W;
                    rootLocalTemp.Z = rootLocalTemp.W;
                }*/

                switch (_boneAttribute)
                {
                    case BoneAttribute.Position:
                        _rootEditsContainer.Translation = new Vector3(rootLocalTemp.X, rootLocalTemp.Y, rootLocalTemp.Z);
                        break;
                    case BoneAttribute.Rotation:
                        _rootEditsContainer.Rotation = new Vector3(rootLocalTemp.X, rootLocalTemp.Y, rootLocalTemp.Z);
                        break;
                    case BoneAttribute.Scale:
                        _rootEditsContainer.Scaling = new Vector3(rootLocalTemp.X, rootLocalTemp.Y, rootLocalTemp.Z);
                        break;
                }

                if (_automaticBoneAttribute)
                {
                    UpdateCurrent("n_root", _rootEditsContainer);
                }
            }

            if (isRootControlDisabled)
            {
                ImGui.EndDisabled();
            }

            var col1Label = "X";
            var col2Label = "Y";
            var col3Label = "Z";
            var col4Label = "All";

            switch (_boneAttribute)
            {
                case BoneAttribute.Position:
                    col4Label = "Unused";
                    break;
                case BoneAttribute.Rotation:
                    col1Label = "Roll";
                    col2Label = "Yaw";
                    col3Label = "Pitch";
                    col4Label = "Unused";
                    break;
            }

            ImGui.Separator();
            ImGui.BeginTable("Bones", 6, ImGuiTableFlags.SizingStretchSame);
            ImGui.TableNextColumn();
            ImGui.Text("Bones:");
            ImGui.TableNextColumn();
            ImGui.Text(col1Label);
            ImGui.TableNextColumn();
            ImGui.Text(col2Label);
            ImGui.TableNextColumn();
            ImGui.Text(col3Label);
            ImGui.TableNextColumn();
            ImGui.Text(col4Label);
            ImGui.TableNextColumn();
            ImGui.Text("Name");
            ImGui.EndTable();

            ImGui.BeginChild("scrolling", new Vector2(0, ImGui.GetFrameHeightWithSpacing() - 56), false);

            for (var i = 0; i < _boneValuesNew.Count; i++)
            {
                var codenameLocal = _boneCodenamesUsed[i];

                var dispNameLocal = _boneDispNamesUsed[i];

                ImGui.PushID(i);

                /*
                if (!BoneData.IsEditableBone(codenameLocal))
                {
                    ImGui.PopID();
                    continue;
                }
                */

                BoneTransform currentEditsContainer = new()
                    { Translation = Vector3.Zero, Rotation = Vector3.Zero, Scaling = Vector3.One };
                var label = "Not Found";

                try
                {
                    if (_boneValuesNew.TryGetValue(codenameLocal, out currentEditsContainer))
                    {
                        label = dispNameLocal;
                    }
                    else if (_boneValuesNew.TryGetValue(dispNameLocal, out currentEditsContainer))
                    {
                        label = dispNameLocal;
                    }
                    else
                    {
                        currentEditsContainer = new BoneTransform
                            { Translation = Vector3.Zero, Rotation = Vector3.Zero, Scaling = Vector3.One };
                    }
                }
                catch (Exception ex)
                {
                }

                var currentVector = Vector3.One;
                switch (_boneAttribute)
                {
                    case BoneAttribute.Position:
                        currentVector = currentEditsContainer.Translation;
                        break;
                    case BoneAttribute.Rotation:
                        currentVector = currentEditsContainer.Rotation;
                        break;
                    case BoneAttribute.Scale:
                        currentVector = currentEditsContainer.Scaling;
                        break;
                }

                if (ImGuiComponents.IconButton(i, FontAwesomeIcon.Recycle))
                {
                    _reset = true;
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Reset");
                }

                if (_reset)
                {
                    BoneTransform editsContainer = null;

                    switch (_boneAttribute)
                    {
                        case BoneAttribute.Position:
                        case BoneAttribute.Rotation:
                            //currentVector.W = 0F;
                            currentVector.X = 0F;
                            currentVector.Y = 0F;
                            currentVector.Z = 0F;
                            break;
                        case BoneAttribute.Scale:
                            //currentVector.W = 1F;
                            currentVector.X = 1F;
                            currentVector.Y = 1F;
                            currentVector.Z = 1F;
                            break;
                    }

                    _reset = false;
                    try
                    {
                        if (_boneValuesNew.TryGetValue(dispNameLocal, out var value))
                        {
                            editsContainer = value;
                        }
                        else if (_boneValuesNew.Remove(codenameLocal, out var removedContainer))
                        {
                            editsContainer = removedContainer;
                            _boneValuesNew[codenameLocal] = editsContainer;
                        }
                        else
                        {
                            throw new Exception();
                        }

                        switch (_boneAttribute)
                        {
                            case BoneAttribute.Position:
                                editsContainer.Translation =
                                    new Vector3(currentVector.X, currentVector.Y, currentVector.Z);
                                break;
                            case BoneAttribute.Rotation:
                                editsContainer.Rotation =
                                    new Vector3(currentVector.X, currentVector.Y, currentVector.Z);
                                break;
                            case BoneAttribute.Scale:
                                editsContainer.Scaling = new Vector3(currentVector.X, currentVector.Y, currentVector.Z);
                                break;
                        }
                    }
                    catch
                    {
                        //throw new Exception();
                    }

                    if (_automaticBoneAttribute)
                    {
                        UpdateCurrent(codenameLocal, editsContainer);
                    }
                }
                /*else if (currentVector.X == currentVector.Y && currentVector.Y == currentVector.Z)
                {
                    currentVector.W = currentVector.X;
                }
                else
                {
                    currentVector.W = 0;
                }*/

                ImGui.SameLine();

                ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 190);

                var minLimit = -10f;
                var maxLimit = 10f;
                var increment = 0.001f;

                switch (_boneAttribute)
                {
                    case BoneAttribute.Rotation:
                        minLimit = -360f;
                        maxLimit = 360f;
                        increment = 1f;
                        break;
                }

                if (ImGui.DragFloat3(label, ref currentVector, increment, minLimit, maxLimit))
                {
                    BoneTransform editsContainer = null;
                    try
                    {
                        if (_reset)
                        {
                            switch (_boneAttribute)
                            {
                                case BoneAttribute.Position:
                                case BoneAttribute.Rotation:
                                    //currentVector.W = 0F;
                                    currentVector.X = 0F;
                                    currentVector.Y = 0F;
                                    currentVector.Z = 0F;
                                    break;
                                case BoneAttribute.Scale:
                                    //currentVector.W = 1F;
                                    currentVector.X = 1F;
                                    currentVector.Y = 1F;
                                    currentVector.Z = 1F;
                                    break;
                            }

                            _reset = false;
                        }
                        /*else if (!((currentVector.X == currentVector.Y) && (currentVector.X == currentVector.Z) && (currentVector.Y == currentVector.Z)))
                        {
                            currentVector.W = 0;
                        }
                        else if (currentVector.W != 0)
                        {
                            currentVector.X = currentVector.W;
                            currentVector.Y = currentVector.W;
                            currentVector.Z = currentVector.W;
                        }*/
                    }
                    catch (Exception ex)
                    {
                    }

                    try
                    {
                        if (_boneValuesNew.TryGetValue(dispNameLocal, out var value))
                        {
                            editsContainer = value;
                        }
                        else if (_boneValuesNew.Remove(codenameLocal, out var removedContainer))
                        {
                            editsContainer = removedContainer;
                            _boneValuesNew[codenameLocal] = editsContainer;
                        }
                        else
                        {
                            throw new Exception();
                        }

                        switch (_boneAttribute)
                        {
                            case BoneAttribute.Position:
                                editsContainer.Translation =
                                    new Vector3(currentVector.X, currentVector.Y, currentVector.Z);
                                break;
                            case BoneAttribute.Rotation:
                                editsContainer.Rotation =
                                    new Vector3(currentVector.X, currentVector.Y, currentVector.Z);
                                break;
                            case BoneAttribute.Scale:
                                editsContainer.Scaling = new Vector3(currentVector.X, currentVector.Y, currentVector.Z);
                                break;
                        }
                    }
                    catch
                    {
                        //throw new Exception();
                    }

                    if (_automaticBoneAttribute)
                    {
                        UpdateCurrent(codenameLocal, editsContainer);
                    }
                }


                ImGui.PopID();
            }

            ImGui.EndChild();

            ImGui.Separator();

            if (ImGui.Button("Save"))
            {
                ApplyViaIPC(_newScaleName, _newScaleCharacter, pi);
            }


            ImGui.SameLine();
            if (ImGui.Button("Reset"))
            {
                RevertToOriginal(_newScaleCharacter, pi);
            }

            ImGui.SameLine();
            if (ImGui.Button("Load from IPC"))
            {
                GetFromIPC(_newScaleCharacter, pi);
            }
        }

        private void ApplyViaIPC(string scaleName, string characterName, DalamudPluginInterface pi)
        {
            //CharacterProfile newBody = new CharacterProfile();
            var newBody = new CharacterProfile();

            for (var i = 0; i < _boneCodenames.Count && i < _boneValuesNew.Count; i++)
            {
                var legacyName = _boneCodenamesUsed[i];

                newBody.Bones[legacyName] = _boneValuesNew[legacyName];
            }

            newBody.Bones["n_root"] = _rootEditsContainer;

            newBody.Enabled = true;
            newBody.ProfileName = "IPC";
            newBody.CharacterName = _newScaleCharacter;

            //newBody.RootScale = new HkVector4(this.newRootScale.X, this.newRootScale.Y, this.newRootScale.Z, 0);

            var bodyString = JsonConvert.SerializeObject(newBody);
            //PluginLog.Information($"{pi.PluginNames}");
            _setCharacterProfile = pi.GetIpcSubscriber<string, string, object>("CustomizePlus.SetCharacterProfile");
            //PluginLog.Information($"{_setCharacterProfile}: -- {bodyString} -- {newBody.CharacterName}");
            _setCharacterProfile.InvokeAction(bodyString, newBody.CharacterName);
        }

        private void GetFromIPC(string characterName, DalamudPluginInterface pi)
        {
            _getCharacterProfile = pi.GetIpcSubscriber<string, string>("CustomizePlus.GetCharacterProfile");
            //PluginLog.Information($"{_setCharacterProfile}: -- {bodyString} -- {newBody.CharacterName}");
            var characterProfileString = _getCharacterProfile.InvokeFunc(_newScaleCharacter);

            //PluginLog.Information(CharacterProfileString);
            if (characterProfileString != null)
            {
                var characterProfile = JsonConvert.DeserializeObject<CharacterProfile?>(characterProfileString);
                PluginLog.Information(
                    $"IPC request for {characterName} found scale named: {characterProfile.ProfileName}");
            }
            else
            {
                PluginLog.Information($"No scale found on IPC request for {characterName}");
            }

            //if (CharacterProfile != null)
            //	this.ScaleUpdated = CharacterProfile;
        }

        private void RevertToOriginal(string characterName, DalamudPluginInterface pi) // Use to unassign override scale in IPC testing mode
        {
            _revert = pi.GetIpcSubscriber<string, object>("CustomizePlus.Revert");
            _revert.InvokeAction(_newScaleCharacter);
        }

        private void UpdateCurrent(string boneName, BoneTransform boneValue)
        {
            var newBody = ScaleUpdated;

            newBody.Bones[boneName] = boneValue;
        }
    }
}