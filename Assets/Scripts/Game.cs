using UnityEngine;
using System.Collections.Generic;

public class Game : MonoBehaviour {
	
	public Transform redBuilding;
	public Transform greenBuilding;
	public Transform avatar;
	public Rigidbody avatarRigidBody;
	public MeshRenderer avatarMeshRenderer;
	public TrailRenderer avatarTrailRenderer;
	public Rigidbody avatarDebris;
	public Material redMaterial;
	public Material greenMaterial;
	public Transform oldBackgroundBuildings;
	public Transform newBackgroundBuildings;
	public GUIText distanceLabel;
	public GUIText highscoreLabel;
	public GUITexture fullscreenOverlayTexture;
	public AudioClip[] jumpAudioClips;
	public AudioClip[] hitAudioClips;
	public AudioClip failAudioClip;
	
	// Private
	const int kMaxBlockBuildings = 50;
	const float kMinBlockWidth = 60;	
	const float kStartAvatarSpeed = 8.0f;
	const float kAvatarSpeedIncCoef = 0.05f;
	const float kMaxAvatarSpeed = 11.0f;
	const float kGravity = 30.0f;
	const float kBounciness = 0.2f;
	const float kJumpEnergyDeplitionRate = 1.0f;
	const float kStartingJumpEnergy = 0.7f;
	const float kJumpEnergyEfficiencyCoef = 50.5f;	
	const float kJumpRotationTime = 0.4f;
	const float kMinJumpImpulse = 4.0f;
	const float kCameraFriction = 0.7f;
	const float kHardestMinBuildingWidth = 4.0f;
	const float kHardestMaxBuildingWidth = 16.0f;
	const float kHardestMaxSpaceWidth = 3.5f;
	const float kFadeToMenuTime = 0.7f;
	const float kMinBuildingGround = -15.0f;
	const float kMaxBuildingGround = 5.0f;
	const float kHardestMaxBuildingGroundDifference = 1.2f;
			
	// Local
	Transform transformRef;
	
	enum ElementColor {
		Red,
		Green,
	}
	
	Vector3 cameraVelocity;
	
	// Blocks
	Transform[] oldBlockBuildings;
	ElementColor[] oldBlockBuildingColors;
	int numberOfOldBlockBuildings;
	Transform[] actualBlockBuildings;
	ElementColor[] actualBlockBuildingColors;	
	int numberOfActualBlockBuildings;
	
	int activeBuildingIndex;
	int activeBlockIndex;
			
	float actualBlockOffset;
	float actualBlockWidth;
	float actualBlockMinBuildingWidth;
	float actualBlockMaxBuildingWidth;
	float actualBlockMinSpaceWidth;
	float actualBlockMaxSpaceWidth;
	float actualBlockMaxBuildingGroundDifference;
	float lastBuildingGround;
	
	float jumpEnergy;
	bool readyForNextJump;
	
	// Avatar
	float avatarYVelocity;
	bool avatarHasContactWithGround; 
	ElementColor avatarColor;
	float jumpRotationTimer;
	float avatarSpeed;
	
	// Menu
	float fadeToMenuTimer;
	int highscore;
	int prevRunDistance;
	int prevHighscore;
	
	// Game states
	enum GameState {
		Playing,
		FadeToMenu,
		Menu,
	};
	
	GameState gameState;
	
	// Use this for initialization
	void Start () {
	
		transformRef = transform;
		
		oldBlockBuildings = new Transform[kMaxBlockBuildings];
		actualBlockBuildings = new Transform[kMaxBlockBuildings];
		oldBlockBuildingColors = new ElementColor[kMaxBlockBuildings];
		actualBlockBuildingColors = new ElementColor[kMaxBlockBuildings];
		
		ResetGame();
	}
	
	//static int screenshotCount = 0;
	
	// Update is called once per frame
	void Update () {
		
		/*
        // take screenshot on up->down transition of F9 key
        if (Input.GetKeyDown("f9"))
        {        
            string screenshotFilename;
            do
            {
                screenshotCount++;
                screenshotFilename = "screenshot" + screenshotCount + ".png";
 
            } while (System.IO.File.Exists(screenshotFilename));
 
            Application.CaptureScreenshot(screenshotFilename);
        }	
        */
		
		// New blocks
		if (avatar.position.x > actualBlockOffset + actualBlockBuildings[0].localScale.x) {
			CreateNewBlock();
		}
		
		// Controlling game
		if (gameState == GameState.Playing) {
						
			// Jump
			if (Input.GetButtonDown("jump") && avatarHasContactWithGround && readyForNextJump) {
				Jump();
			}			
			if (Input.GetButtonUp("jump")) {
				jumpEnergy = 0.0f;
				readyForNextJump = true;
			}			
			
			// Switch
			if (Input.GetButtonDown("switch")) {
				if (avatarColor == ElementColor.Green) {
					avatarMeshRenderer.material = redMaterial;
					avatarTrailRenderer.material = redMaterial;
					avatarColor = ElementColor.Red;
				}
				else {
					avatarMeshRenderer.material = greenMaterial;
					avatarTrailRenderer.material = greenMaterial;
					avatarColor = ElementColor.Green;
				}
			}
		}
		else if (gameState == GameState.FadeToMenu) {
			
			fullscreenOverlayTexture.color = new Color(0.5f, 0.5f, 0.5f, 0.5f * fadeToMenuTimer / kFadeToMenuTime);
			
			fadeToMenuTimer += Time.deltaTime;
			if (fadeToMenuTimer >= kFadeToMenuTime) {
				gameState = GameState.Menu;
				fullscreenOverlayTexture.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
				avatarRigidBody.isKinematic = true;
			}
		}
		else if (gameState == GameState.Menu) {
			if (Input.anyKeyDown) {
				ResetGame();
			}
		}
		
		// GUI data
		int runDistance = Mathf.RoundToInt(avatar.position.x * 0.5f);
		if (highscore < runDistance) {
			highscore = runDistance;
		}
		if (prevRunDistance != runDistance) {
			distanceLabel.text = System.Convert.ToString(runDistance);
			prevRunDistance = runDistance;
		}
		
		if (prevHighscore != highscore) {
			highscoreLabel.text = "Highscore:" + System.Convert.ToString(highscore);
			prevHighscore = highscore;
		}
	}
	
	void FixedUpdate() {
						
		// Camera pos
		Vector3 cameraPosition = transformRef.position;
		Vector3 designatedPosition = new Vector3(avatar.position.x + 3.5f, avatar.position.y + 0.2f, cameraPosition.z);
		cameraVelocity += (designatedPosition - cameraPosition) * Time.deltaTime * 10.0f;
		cameraVelocity *= kCameraFriction;
		cameraPosition += cameraVelocity * Time.deltaTime * 10.0f;
		cameraPosition.x = avatar.position.x + 3.4f;
		transformRef.position = cameraPosition;		
		
		// Avatar physics
		if (gameState == GameState.Playing) {
						
			// Avatar speed
			if (avatarSpeed < kMaxAvatarSpeed) {
				avatarSpeed += kAvatarSpeedIncCoef * Time.deltaTime;						
				if (avatarSpeed > kMaxAvatarSpeed) {
					avatarSpeed = kMaxAvatarSpeed;
				}
			}
		
			// Jumping
			if (Input.GetButton("jump") && jumpEnergy > 0) {
				avatarYVelocity += jumpEnergy * kJumpEnergyEfficiencyCoef * Time.deltaTime;
				jumpEnergy -= kJumpEnergyDeplitionRate * Time.deltaTime;
			}

			bool potentialFailCollision = (!IsAvatarInActiveBuildingColumn() && (avatar.position.y - avatar.localScale.y * 0.5f < GetNextBuildingGroundPosition()));
						
			avatarYVelocity -= kGravity * Time.deltaTime;
			
			Vector3 position = avatar.position;
			Vector3 prevPosition = position;
			position.x += Time.deltaTime * avatarSpeed;
			position.y += Time.deltaTime * avatarYVelocity;
			avatar.position = position;
			
			// Rotation
			if (!avatarHasContactWithGround && jumpRotationTimer < kJumpRotationTime) {
				float t = Mathf.Sin(jumpRotationTimer / kJumpRotationTime * Mathf.PI);
				Quaternion rotation = avatar.rotation;
				rotation.z = - t * Mathf.PI * 0.05f;
				avatar.rotation = rotation;
				jumpRotationTimer += Time.deltaTime;
			}
			else {
				Quaternion rotation = avatar.rotation;
				rotation.z = 0.0f;
				avatar.rotation = rotation;				
			}
			
			float groundPosition = GetActiveBuildingGroundPosition();
			if (IsAvatarInActiveBuildingColumn()) {
							
				if (position.y - avatar.localScale.y * 0.5f < groundPosition) {
					
					if (!avatarHasContactWithGround) {
						AudioSource.PlayClipAtPoint(hitAudioClips[Random.Range(0, hitAudioClips.Length)], avatar.position);						
					}
					
					avatarHasContactWithGround = true;
					jumpRotationTimer = kJumpRotationTime;
					
					// Fail 
					if (potentialFailCollision || GetActiveBuildingColor() != avatarColor) {
						FadeToMenu();
						avatar.position = prevPosition;
						avatarRigidBody.isKinematic = false;						
						avatarRigidBody.AddForce(new Vector3(avatarSpeed * 40.0f, avatarYVelocity * 20.0f, 0.0f));
						AudioSource.PlayClipAtPoint(failAudioClip, avatar.position);		
						CreateFailDebris();
					}
					// Bounce
					else {	
						jumpEnergy = 0;
						if (avatarYVelocity < 0) {
							avatarYVelocity = -avatarYVelocity * kBounciness;
						}
						position.y = groundPosition + avatar.localScale.y * 0.5f;
						avatar.position = position;
					}
					
					if (Input.GetButton("jump") && readyForNextJump) {
						Jump();
					}					
				}
			}
			else {
				avatarHasContactWithGround = false;
			}
		}
	}
	
	void FadeToMenu() {
		
		gameState = GameState.FadeToMenu;
		fullscreenOverlayTexture.enabled = true;
		fullscreenOverlayTexture.color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
		fadeToMenuTimer = 0;
	}
	
	void ResetGame() {
		
		fullscreenOverlayTexture.enabled = false;
		
		actualBlockOffset = 0;
		actualBlockWidth = 0;
		activeBuildingIndex = 0;
		activeBlockIndex = 0;
		
		actualBlockMinBuildingWidth = 18;
		actualBlockMaxBuildingWidth = 20;
		actualBlockMinSpaceWidth = 1.4f;
		actualBlockMaxSpaceWidth = 1.6f;
		actualBlockMaxBuildingGroundDifference = 0.5f;
		lastBuildingGround = 0.0f;
		
		CreateNewBlock();
		CreateNewBlock();
		
		avatarColor = oldBlockBuildingColors[0];
		if (avatarColor == ElementColor.Green) {
			avatarMeshRenderer.material = greenMaterial;
			avatarTrailRenderer.material = greenMaterial;
		}
		else {
			avatarMeshRenderer.material = redMaterial;
			avatarTrailRenderer.material = redMaterial;
		}
		
		avatarSpeed = kStartAvatarSpeed;
		jumpEnergy = kStartingJumpEnergy;
		readyForNextJump = true;
		
		avatarRigidBody.isKinematic = true;
		avatar.transform.rotation = Quaternion.identity;
		avatar.position = new Vector3(1.0f, 1.5f, 0.0f);
		
		transform.position = new Vector3(8.0f, 1.2f, -30.0f);
		
		gameState = GameState.Playing;		
	}
	
	void Jump() {
		jumpEnergy = kStartingJumpEnergy;
		avatarHasContactWithGround = false;
		jumpRotationTimer = 0;
		avatarYVelocity += kMinJumpImpulse;
		readyForNextJump  = false;	
		AudioSource.PlayClipAtPoint(jumpAudioClips[Random.Range(0, jumpAudioClips.Length)], avatar.position);
	}
	
	void CreateNewBlock() {
		
		// Switch BG
		Transform bgBuildings = oldBackgroundBuildings;
		oldBackgroundBuildings = newBackgroundBuildings;
		newBackgroundBuildings = bgBuildings;
		
		// Update active building index
		if (activeBuildingIndex >= numberOfOldBlockBuildings) {
			activeBuildingIndex -= numberOfOldBlockBuildings;
		}
		// Delete objects in old block
		for (int i = 0; i < kMaxBlockBuildings; i++) {
			if (oldBlockBuildings[i]) {
				Destroy(oldBlockBuildings[i].gameObject);
				oldBlockBuildings[i] = null;
			}
		}
				
		System.Array.Copy(actualBlockBuildings, oldBlockBuildings, numberOfActualBlockBuildings);
		System.Array.Copy(actualBlockBuildingColors, oldBlockBuildingColors, numberOfActualBlockBuildings);
		numberOfOldBlockBuildings = numberOfActualBlockBuildings;
		numberOfActualBlockBuildings = 0;
		
		// New params
		if (actualBlockMinBuildingWidth > kHardestMinBuildingWidth) {
			actualBlockMinBuildingWidth -= 2.0f;
			if (actualBlockMinBuildingWidth < kHardestMinBuildingWidth) {			
				actualBlockMinBuildingWidth = kHardestMinBuildingWidth;
			}
		}
		
		if (actualBlockMaxBuildingWidth > kHardestMaxBuildingWidth) {
			actualBlockMaxBuildingWidth -= 2.0f;
			if (actualBlockMaxBuildingWidth < kHardestMaxBuildingWidth) {			
				actualBlockMaxBuildingWidth = kHardestMaxBuildingWidth;
			}
		}
		
		if (actualBlockMaxSpaceWidth < kHardestMaxSpaceWidth) {
			actualBlockMaxSpaceWidth += 0.5f;
			if (actualBlockMaxSpaceWidth > kHardestMaxSpaceWidth) {		
				actualBlockMaxSpaceWidth = kHardestMaxSpaceWidth;
			}
		}
				
		if (actualBlockMaxBuildingGroundDifference < kHardestMaxBuildingGroundDifference) {
			actualBlockMaxBuildingGroundDifference += 0.1f;
			if (actualBlockMaxBuildingGroundDifference > kHardestMaxBuildingGroundDifference) {			
				actualBlockMaxBuildingGroundDifference = kHardestMaxBuildingGroundDifference;
			}
		}
		
		actualBlockOffset += actualBlockWidth;
		float offset = actualBlockOffset;
		actualBlockWidth = 0;
		
		bool creatingSpace = true;
		while (actualBlockWidth < kMinBlockWidth || !creatingSpace) {
			
			// Space
			if (creatingSpace) {
				float spaceWidth = Random.Range(actualBlockMinSpaceWidth, actualBlockMaxSpaceWidth);
				offset += spaceWidth;
				actualBlockWidth += spaceWidth;
			}
			// Building
			else {
				if (numberOfActualBlockBuildings == kMaxBlockBuildings - 1) {
					break;
				}
				else {
					float buildingWidth = Random.Range(actualBlockMinBuildingWidth, actualBlockMaxBuildingWidth);
					
					if (activeBlockIndex % 4 == 0) {
						lastBuildingGround += actualBlockMaxBuildingGroundDifference * 0.3f;
						buildingWidth = actualBlockMinBuildingWidth * 1.2f;
					}
					else if ((activeBlockIndex - 2) % 4 == 0 && activeBlockIndex > 4) {
						lastBuildingGround -= actualBlockMaxBuildingGroundDifference * 0.2f;
						buildingWidth = actualBlockMinBuildingWidth * 1.7f;
					}
					else {
						lastBuildingGround += Random.Range(-actualBlockMaxBuildingGroundDifference, actualBlockMaxBuildingGroundDifference);
					}
					
					if (lastBuildingGround < kMinBuildingGround) {
						lastBuildingGround = kMinBuildingGround;
					}
					else if (lastBuildingGround > kMaxBuildingGround) {
						lastBuildingGround = kMaxBuildingGround;
					}
					
					ElementColor color = (Random.Range(0, 2) == 0) ? ElementColor.Red : ElementColor.Green;
					Transform building = (color == ElementColor.Red ? redBuilding : greenBuilding);
					Transform newBuilding = (Transform)Instantiate(building, new Vector3((offset + buildingWidth * 0.5f), lastBuildingGround - 25.0f, 0.0f), Quaternion.identity);
					newBuilding.transform.localScale = new Vector3(buildingWidth, 50.0f, 0.2f);
					actualBlockBuildings[numberOfActualBlockBuildings] = newBuilding;
					actualBlockBuildingColors[numberOfActualBlockBuildings] = color;
					numberOfActualBlockBuildings++;
					offset += buildingWidth;
					actualBlockWidth += buildingWidth;
				}
			}
			
			creatingSpace = !creatingSpace;
		}
		
		Vector3 bgBuildingsPos = newBackgroundBuildings.position;
		bgBuildingsPos.x = actualBlockOffset + actualBlockWidth * 0.5f;
		newBackgroundBuildings.position = bgBuildingsPos;
		
		Vector3 bgBuildingsScale = newBackgroundBuildings.localScale;
		bgBuildingsScale.x = actualBlockWidth;
		newBackgroundBuildings.localScale = bgBuildingsScale;
		
		activeBlockIndex++;
	}
	
	void CreateFailDebris() {
		for (int i = 0; i < 30; i++) {
			Vector3 pos = Random.onUnitSphere * 0.2f + avatar.position;
			Rigidbody debris = (Rigidbody)Instantiate(avatarDebris, pos, Quaternion.identity);
			debris.AddForce(new Vector3(avatarSpeed * 40.0f, avatarYVelocity * 20.0f, 0.0f));
			MeshRenderer debrisMeshRenderer = debris.GetComponent<MeshRenderer>();
			if (avatarColor == ElementColor.Green) {
				debrisMeshRenderer.material = greenMaterial;
			}
			else {
				debrisMeshRenderer.material = redMaterial;				
			}
		}
	}
	
	Transform GetBuildingForIdx(int buildingIdx) {
		
		if (buildingIdx < numberOfOldBlockBuildings) {
			return oldBlockBuildings[buildingIdx];
		}
		else {
			return actualBlockBuildings[buildingIdx - numberOfOldBlockBuildings];
		}
	}
	
	ElementColor GetBuildingColorForIdx(int buildingIdx) {
		if (buildingIdx < numberOfOldBlockBuildings) {
			return oldBlockBuildingColors[buildingIdx];
		}
		else {
			return actualBlockBuildingColors[buildingIdx - numberOfOldBlockBuildings];
		}		
	}
	
	Transform GetActiveBuilding() {
		
		Transform nextBuilding = GetBuildingForIdx(activeBuildingIndex + 1);
		while (avatar.position.x + avatar.localScale.x * 0.5f > nextBuilding.position.x - nextBuilding.localScale.x * 0.5f) {
			activeBuildingIndex++;
			nextBuilding = GetBuildingForIdx(activeBuildingIndex + 1);
		}
		return GetBuildingForIdx(activeBuildingIndex);		
	}
	
	ElementColor GetActiveBuildingColor() {
		
		Transform nextBuilding = GetBuildingForIdx(activeBuildingIndex + 1);
		while (avatar.position.x + avatar.localScale.x * 0.5f > nextBuilding.position.x - nextBuilding.localScale.x * 0.5f) {
			activeBuildingIndex++;
			nextBuilding = GetBuildingForIdx(activeBuildingIndex + 1);
		}
		return GetBuildingColorForIdx(activeBuildingIndex);		
	}
	
	Transform GetNextBuilding() {
		
		int nextBuildingIndex = activeBuildingIndex + 1;
		Transform nextBuilding = GetBuildingForIdx(nextBuildingIndex);
		while (avatar.position.x + avatar.localScale.x * 0.5f > nextBuilding.position.x - nextBuilding.localScale.x * 0.5f) {
			nextBuildingIndex++;
			nextBuilding = GetBuildingForIdx(nextBuildingIndex);
		}
		return nextBuilding;	
	}	
	
	bool IsAvatarInActiveBuildingColumn() {
		
		Transform activeBuilding = GetActiveBuilding();
		return (avatar.position.x - avatar.localScale.x * 0.5 < activeBuilding.position.x + activeBuilding.localScale.x * 0.5f);
	}
	
	float GetActiveBuildingGroundPosition() {

		Transform activeBuilding = GetActiveBuilding();
		return activeBuilding.position.y + activeBuilding.localScale.y * 0.5f;
	}
	
	float GetNextBuildingGroundPosition() {

		Transform nextBuilding = GetNextBuilding();
		return nextBuilding.position.y + nextBuilding.localScale.y * 0.5f;
	}	
}
