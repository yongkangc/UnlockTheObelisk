# UnlockTheObelisk Makefile
# Simplifies building and running the save editor

# Detect OS and set paths
UNAME := $(shell uname)
ifeq ($(UNAME), Darwin)
    # macOS
    DOTNET := /opt/homebrew/opt/dotnet@6/bin/dotnet
    SAVE_DIR := $(HOME)/Library/Application Support/Dreamsite Games/AcrossTheObelisk
    GAME_DIR := $(HOME)/Library/Application Support/Steam/steamapps/common/Across the Obelisk/Contents/Resources/Data/Managed
else
    # Windows/Linux
    DOTNET := dotnet
    SAVE_DIR := $(APPDATA)/../LocalLow/Dreamsite Games/AcrossTheObelisk
    GAME_DIR :=
endif

DLL := ATOUnlocker/bin/Debug/net6.0/ATOUnlocker.dll
STEAM_ID ?= $(shell ls "$(SAVE_DIR)" 2>/dev/null | grep -E '^[0-9]+$$' | head -1)
SAVE_FILE := $(SAVE_DIR)/$(STEAM_ID)/player.ato

.PHONY: build run tui cli-all cli-heroes cli-town cli-perks backup backup-full restore restore-full clean help find-save

# Default target
help:
	@echo "UnlockTheObelisk - Across the Obelisk Save Editor"
	@echo ""
	@echo "Usage:"
	@echo "  make build       - Build the project"
	@echo "  make run         - Launch interactive TUI editor"
	@echo "  make tui         - Same as 'make run'"
	@echo ""
	@echo "Quick unlock commands:"
	@echo "  make cli-all     - Unlock everything (heroes, town, perks)"
	@echo "  make cli-heroes  - Unlock all heroes"
	@echo "  make cli-town    - Unlock all town upgrades"
	@echo "  make cli-perks   - Max out all perk points"
	@echo ""
	@echo "Save management:"
	@echo "  make backup      - Backup player.ato only"
	@echo "  make backup-full - Backup ALL save files (player, runs, perks, gamedata)"
	@echo "  make restore     - Restore player.ato from backup"
	@echo "  make restore-full - Restore ALL save files from backup"
	@echo "  make find-save   - Show detected save file path"
	@echo ""
	@echo "Other:"
	@echo "  make clean       - Clean build artifacts"
	@echo ""
	@echo "Detected Steam ID: $(STEAM_ID)"
	@echo "Save file: $(SAVE_FILE)"

# Build the project
build:
	@echo "Building..."
	@$(DOTNET) build ATOUnlocker.sln
	@echo "Build complete!"

# Run interactive TUI (default mode)
run: build
	@if [ -z "$(STEAM_ID)" ]; then \
		echo "Error: Could not detect Steam ID. Check your save directory."; \
		exit 1; \
	fi
	@if [ ! -f "$(SAVE_FILE)" ]; then \
		echo "Error: Save file not found at $(SAVE_FILE)"; \
		exit 1; \
	fi
	@$(DOTNET) "$(DLL)" "$(SAVE_FILE)"

tui: run

# CLI quick commands
cli-all: build backup
	@echo "Unlocking everything..."
	@$(DOTNET) "$(DLL)" "$(SAVE_FILE)" heroes town perks
	@echo "Done!"

cli-heroes: build backup
	@echo "Unlocking all heroes..."
	@$(DOTNET) "$(DLL)" "$(SAVE_FILE)" heroes
	@echo "Done!"

cli-town: build backup
	@echo "Unlocking all town upgrades..."
	@$(DOTNET) "$(DLL)" "$(SAVE_FILE)" town
	@echo "Done!"

cli-perks: build backup
	@echo "Maxing out perk points..."
	@$(DOTNET) "$(DLL)" "$(SAVE_FILE)" perks
	@echo "Done!"

# Backup save file
backup:
	@if [ -z "$(STEAM_ID)" ]; then \
		echo "Error: Could not detect Steam ID."; \
		exit 1; \
	fi
	@if [ -f "$(SAVE_FILE)" ]; then \
		cp "$(SAVE_FILE)" "$(SAVE_FILE).backup_$$(date +%Y%m%d_%H%M%S)"; \
		echo "Backup created: $(SAVE_FILE).backup_$$(date +%Y%m%d_%H%M%S)"; \
	else \
		echo "Save file not found: $(SAVE_FILE)"; \
	fi

# Restore player.ato from most recent backup
restore:
	@if [ -z "$(STEAM_ID)" ]; then \
		echo "Error: Could not detect Steam ID."; \
		exit 1; \
	fi
	@LATEST=$$(ls -t "$(SAVE_DIR)/$(STEAM_ID)"/player.ato.backup_* 2>/dev/null | head -1); \
	if [ -n "$$LATEST" ]; then \
		cp "$$LATEST" "$(SAVE_FILE)"; \
		echo "Restored from: $$LATEST"; \
	else \
		echo "No backup found"; \
	fi

# Full backup - all save files to timestamped directory
backup-full:
	@if [ -z "$(STEAM_ID)" ]; then \
		echo "Error: Could not detect Steam ID."; \
		exit 1; \
	fi
	@BACKUP_DIR="$(SAVE_DIR)/$(STEAM_ID)/backup_$$(date +%Y%m%d_%H%M%S)"; \
	mkdir -p "$$BACKUP_DIR"; \
	echo "Creating full backup in: $$BACKUP_DIR"; \
	for f in player.ato runs.ato perks.ato gamedata_0.ato gamedata_1.ato; do \
		if [ -f "$(SAVE_DIR)/$(STEAM_ID)/$$f" ]; then \
			cp "$(SAVE_DIR)/$(STEAM_ID)/$$f" "$$BACKUP_DIR/"; \
			echo "  Backed up: $$f"; \
		fi; \
	done; \
	echo "Full backup complete!"

# Restore all save files from most recent full backup
restore-full:
	@if [ -z "$(STEAM_ID)" ]; then \
		echo "Error: Could not detect Steam ID."; \
		exit 1; \
	fi
	@LATEST_DIR=$$(ls -td "$(SAVE_DIR)/$(STEAM_ID)"/backup_* 2>/dev/null | head -1); \
	if [ -n "$$LATEST_DIR" ] && [ -d "$$LATEST_DIR" ]; then \
		echo "Restoring from: $$LATEST_DIR"; \
		for f in player.ato runs.ato perks.ato gamedata_0.ato gamedata_1.ato; do \
			if [ -f "$$LATEST_DIR/$$f" ]; then \
				cp "$$LATEST_DIR/$$f" "$(SAVE_DIR)/$(STEAM_ID)/"; \
				echo "  Restored: $$f"; \
			fi; \
		done; \
		echo "Full restore complete!"; \
	else \
		echo "No full backup found. Use 'make backup-full' first."; \
	fi

# Show detected save file
find-save:
	@echo "Save directory: $(SAVE_DIR)"
	@echo "Detected Steam ID: $(STEAM_ID)"
	@echo "Save file: $(SAVE_FILE)"
	@if [ -f "$(SAVE_FILE)" ]; then \
		echo "Status: Found"; \
	else \
		echo "Status: Not found"; \
	fi

# Clean build artifacts
clean:
	@$(DOTNET) clean ATOUnlocker.sln
	@rm -rf ATOUnlocker/bin ATOUnlocker/obj
	@echo "Cleaned!"
