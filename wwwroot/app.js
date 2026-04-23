// FileBrowser Class - Pure JavaScript (No TypeScript)
class FileBrowser {
    constructor() {
        this.currentPath = "";
        this.apiBaseUrl = "/api/filebrowser";

        // Initialize DOM Elements
        this.modal = document.getElementById("fileBrowserModal");
        this.openBtn = document.getElementById("openBrowserBtn");
        this.closeBtn = document.getElementById("closeModal");
        this.breadcrumb = document.getElementById("breadcrumb");
        this.folderContainer = document.getElementById("folderContainer");
        this.fileContainer = document.getElementById("fileContainer");
        this.searchResults = document.getElementById("searchResults");
        this.searchInput = document.getElementById("searchInput");
        this.searchBtn = document.getElementById("searchBtn");
        this.clearSearchBtn = document.getElementById("clearSearchBtn");
        this.uploadBtn = document.getElementById("uploadBtn");
        this.newFolderBtn = document.getElementById("newFolderBtn");
        this.fileInput = document.getElementById("fileInput");
        this.loadingIndicator = document.getElementById("loadingIndicator");
        this.errorMessage = document.getElementById("errorMessage");
        this.fileCount = document.getElementById("fileCount");
        this.folderCount = document.getElementById("folderCount");
        this.totalSize = document.getElementById("totalSize");

        console.log("FileBrowser initialized");
        this.setupEventListeners();
        this.loadFromUrl();
    }

    setupEventListeners() {
        console.log("Setting up event listeners");

        this.openBtn.addEventListener("click", () => {
            console.log("Open button clicked");
            this.openModal();
        });

        this.closeBtn.addEventListener("click", () => this.closeModal());
        this.searchBtn.addEventListener("click", () => this.handleSearch());
        this.clearSearchBtn.addEventListener("click", () => this.clearSearch());
        this.uploadBtn.addEventListener("click", () => this.fileInput.click());
        this.newFolderBtn.addEventListener("click", () => this.handleNewFolder());
        this.fileInput.addEventListener("change", () => this.handleFileUpload());

        this.searchInput.addEventListener("keyup", (e) => {
            if (e.key === "Enter") {
                this.handleSearch();
            }
        });

        window.addEventListener("hashchange", () => this.loadFromUrl());

        this.modal.addEventListener("click", (e) => {
            if (e.target === this.modal) {
                this.closeModal();
            }
        });
    }

    openModal() {
        console.log("Opening modal");
        this.modal.classList.remove("hidden");
        this.loadDirectory();
    }

    closeModal() {
        this.modal.classList.add("hidden");
    }

    loadFromUrl() {
        const hash = window.location.hash.slice(1);
        this.currentPath = hash || "";

        if (!this.modal.classList.contains("hidden")) {
            this.loadDirectory();
        }
    }

    async loadDirectory() {
        try {
            this.showLoading(true);
            this.hideError();

            const url = `${this.apiBaseUrl}/browse?path=${encodeURIComponent(this.currentPath)}`;
            console.log("Fetching:", url);

            const response = await fetch(url);

            if (!response.ok) {
                throw new Error("Failed to load directory");
            }

            const data = await response.json();
            console.log("Directory data:", data);
            this.renderBrowse(data);
            return Promise.resolve();  // ← Add this
        } catch (error) {
            console.error("Error loading directory:", error);
            this.showError(error.message || "Unknown error");
            return Promise.reject(error);  // ← Add this
        } finally {
            this.showLoading(false);
        }
    }

    async handleSearch() {
        const query = this.searchInput.value.trim();

        if (!query) {
            this.showError("Please enter a search query");
            return;
        }

        try {
            this.showLoading(true);
            this.hideError();

            const url = `${this.apiBaseUrl}/search?query=${encodeURIComponent(query)}&path=${encodeURIComponent(this.currentPath)}&maxResults=200`;
            const response = await fetch(url);

            if (!response.ok) {
                throw new Error("Search failed");
            }

            const data = await response.json();
            this.renderSearch(data);
        } catch (error) {
            this.showError(error.message || "Unknown error");
        } finally {
            this.showLoading(false);
        }
    }

    clearSearch() {
        this.searchInput.value = "";
        this.searchResults.classList.add("hidden");
        this.folderContainer.classList.remove("hidden");
        this.fileContainer.classList.remove("hidden");
    }

    async handleFileUpload() {
        const files = this.fileInput.files;

        if (!files || files.length === 0) {
            return;
        }

        try {
            this.showLoading(true);
            this.hideError();

            const formData = new FormData();
            for (let i = 0; i < files.length; i++) {
                formData.append("files", files[i]);
            }

            const url = `${this.apiBaseUrl}/upload?path=${encodeURIComponent(this.currentPath)}`;
            const response = await fetch(url, {
                method: "POST",
                body: formData
            });

            if (!response.ok) {
                throw new Error("Upload failed");
            }

            this.fileInput.value = "";
            await this.loadDirectory();
        } catch (error) {
            this.showError(error.message || "Upload failed");
        } finally {
            this.showLoading(false);
        }
    }

    async handleNewFolder() {
        const folderName = prompt("Enter folder name:");

        if (!folderName || !folderName.trim()) {
            return;
        }

        try {
            this.showLoading(true);
            this.hideError();

            const response = await fetch(`${this.apiBaseUrl}/createfolder?folderName=${encodeURIComponent(folderName)}&path=${encodeURIComponent(this.currentPath)}`, {
                method: "POST"
            });

            if (!response.ok) {
                throw new Error("Failed to create folder");
            }

            await this.loadDirectory();
        } catch (error) {
            this.showError(error.message || "Failed to create folder");
        } finally {
            this.showLoading(false);
        }
    }

    renderBrowse(data) {
        this.folderContainer.innerHTML = "";
        this.fileContainer.innerHTML = "";
        this.searchResults.classList.add("hidden");
        this.folderContainer.classList.remove("hidden");
        this.fileContainer.classList.remove("hidden");

        // Render folders
        for (const folder of data.folders) {
            const folderEl = this.createFolderElement(folder);
            this.folderContainer.appendChild(folderEl);
        }

        // Render files
        for (const file of data.files) {
            const fileEl = this.createFileElement(file);
            this.fileContainer.appendChild(fileEl);
        }

        // Update breadcrumb and stats
        this.updateBreadcrumb(data.path);
        this.updateStats(data);
    }

    renderSearch(data) {
        this.folderContainer.classList.add("hidden");
        this.fileContainer.classList.add("hidden");
        this.searchResults.classList.remove("hidden");
        this.searchResults.innerHTML = "";

        if (data.results.length === 0) {
            this.searchResults.innerHTML = '<div style="grid-column: 1/-1; text-align: center; padding: 40px; color: #999;">No results found</div>';
            return;
        }

        for (const result of data.results) {
            const resultEl = this.createSearchResultElement(result);
            this.searchResults.appendChild(resultEl);
        }
    }

    createFolderElement(folder) {
        const div = document.createElement("div");
        div.className = "item";

        const icon = document.createElement("div");
        icon.className = "item-icon";
        icon.textContent = "📁";

        const name = document.createElement("div");
        name.className = "item-name";
        name.textContent = folder.name;

        const details = document.createElement("div");
        details.className = "item-details";
        details.textContent = `${folder.fileCount} files, ${folder.folderCount} folders`;

        const actions = document.createElement("div");
        actions.className = "item-actions";

        const openBtn = document.createElement("button");
        openBtn.className = "item-action-btn";
        openBtn.textContent = "Open";
        openBtn.addEventListener("click", () => this.navigateToFolder(folder.name));

        const deleteBtn = document.createElement("button");
        deleteBtn.className = "item-action-btn delete";
        deleteBtn.textContent = "Delete";
        deleteBtn.addEventListener("click", () => this.deleteItem(folder.name, true));

        actions.appendChild(openBtn);
        actions.appendChild(deleteBtn);

        div.appendChild(icon);
        div.appendChild(name);
        div.appendChild(details);
        div.appendChild(actions);

        return div;
    }

    createFileElement(file) {
        const div = document.createElement("div");
        div.className = "item";

        const icon = document.createElement("div");
        icon.className = "item-icon";
        icon.textContent = this.getFileIcon(file.name);

        const name = document.createElement("div");
        name.className = "item-name";
        name.textContent = file.name;

        const details = document.createElement("div");
        details.className = "item-details";
        details.textContent = this.formatFileSize(file.size);

        const actions = document.createElement("div");
        actions.className = "item-actions";

        const downloadBtn = document.createElement("button");
        downloadBtn.className = "item-action-btn";
        downloadBtn.textContent = "Download";
        downloadBtn.addEventListener("click", () => this.downloadFile(file.name));

        const deleteBtn = document.createElement("button");
        deleteBtn.className = "item-action-btn delete";
        deleteBtn.textContent = "Delete";
        deleteBtn.addEventListener("click", () => this.deleteItem(file.name, false));

        actions.appendChild(downloadBtn);
        actions.appendChild(deleteBtn);

        div.appendChild(icon);
        div.appendChild(name);
        div.appendChild(details);
        div.appendChild(actions);

        return div;
    }

    createSearchResultElement(result) {
        const div = document.createElement("div");
        div.className = "item";

        const icon = document.createElement("div");
        icon.className = "item-icon";
        icon.textContent = result.type === "folder" ? "📁" : this.getFileIcon(result.name);

        const name = document.createElement("div");
        name.className = "item-name";
        name.textContent = result.name;

        const details = document.createElement("div");
        details.className = "item-details";
        if (result.type === "folder") {
            details.textContent = `${result.fileCount} files`;
        } else {
            details.textContent = this.formatFileSize(result.size || 0);
        }

        const path = document.createElement("div");
        path.className = "item-details";
        path.style.fontSize = "11px";
        path.style.color = "#aaa";
        path.textContent = result.fullPath;

        const actions = document.createElement("div");
        actions.className = "item-actions";

        if (result.type === "folder") {
            const openBtn = document.createElement("button");
            openBtn.className = "item-action-btn";
            openBtn.textContent = "Open";
            openBtn.addEventListener("click", () => this.navigateToPath(result.fullPath));
            actions.appendChild(openBtn);
        } else {
            const downloadBtn = document.createElement("button");
            downloadBtn.className = "item-action-btn";
            downloadBtn.textContent = "Download";
            downloadBtn.addEventListener("click", () => this.downloadFileFromPath(result.fullPath, result.name));
            actions.appendChild(downloadBtn);
        }

        div.appendChild(icon);
        div.appendChild(name);
        div.appendChild(details);
        div.appendChild(path);
        div.appendChild(actions);

        return div;
    }

    navigateToFolder(folderName) {
        const newPath = this.currentPath ? `${this.currentPath}/${folderName}` : folderName;
        const oldPath = this.currentPath;
        this.currentPath = newPath;

        this.loadDirectory().catch(() => {
            this.currentPath = oldPath;
        }).finally(() => {
            this.updateUrl();
        });
    }

    navigateToPath(path) {
        const oldPath = this.currentPath;
        this.currentPath = path;

        this.loadDirectory().catch(() => {
            this.currentPath = oldPath;
        }).finally(() => {
            this.updateUrl();
        });
    }

    async downloadFile(fileName) {
        const url = `${this.apiBaseUrl}/download?file=${encodeURIComponent(fileName)}&path=${encodeURIComponent(this.currentPath)}`;
        const link = document.createElement("a");
        link.href = url;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    }

    async downloadFileFromPath(filePath, fileName) {
        const pathParts = filePath.split("/");
        const actualFileName = pathParts.pop() || fileName;
        const dirPath = pathParts.join("/");

        const url = `${this.apiBaseUrl}/download?file=${encodeURIComponent(actualFileName)}&path=${encodeURIComponent(dirPath)}`;
        const link = document.createElement("a");
        link.href = url;
        link.download = actualFileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    }

    async deleteItem(name, isFolder) {
        const confirmMessage = isFolder 
            ? `Delete folder "${name}" and all its contents?`
            : `Delete file "${name}"?`;

        if (!confirm(confirmMessage)) {
            return;
        }

        try {
            this.showLoading(true);
            this.hideError();

            const url = `${this.apiBaseUrl}/delete?name=${encodeURIComponent(name)}&path=${encodeURIComponent(this.currentPath)}&isFolder=${isFolder}`;
            const response = await fetch(url, { method: "DELETE" });

            if (!response.ok) {
                throw new Error("Delete failed");
            }

            await this.loadDirectory();
        } catch (error) {
            this.showError(error.message || "Failed to delete item");
        } finally {
            this.showLoading(false);
        }
    }

    updateBreadcrumb(path) {
        this.breadcrumb.innerHTML = "";

        const homeBtn = document.createElement("span");
        homeBtn.className = "breadcrumb-item";
        homeBtn.textContent = "Home";
        homeBtn.addEventListener("click", () => {
            this.currentPath = "";
            this.updateUrl();
            this.loadDirectory();
        });

        this.breadcrumb.appendChild(homeBtn);

        if (path) {
            const parts = path.split("/").filter(p => p);

            for (let i = 0; i < parts.length; i++) {
                const separator = document.createElement("span");
                separator.className = "breadcrumb-separator";
                separator.textContent = "/";
                this.breadcrumb.appendChild(separator);

                const item = document.createElement("span");
                item.className = "breadcrumb-item";
                item.textContent = parts[i];

                const targetPath = parts.slice(0, i + 1).join("/");
                item.addEventListener("click", () => {
                    this.currentPath = targetPath;
                    this.updateUrl();
                    this.loadDirectory();
                });

                this.breadcrumb.appendChild(item);
            }
        }
    }

    updateStats(data) {
        this.fileCount.textContent = data.totalFiles.toString();
        this.folderCount.textContent = data.totalFolders.toString();
        this.totalSize.textContent = this.formatFileSize(data.totalSize);
    }

    updateUrl() {
        window.location.hash = this.currentPath;
    }

    getFileIcon(fileName) {
        const ext = fileName.split(".").pop().toLowerCase();
        const iconMap = {
            "pdf": "📄",
            "doc": "📝", "docx": "📝", "txt": "📝",
            "xls": "📊", "xlsx": "📊", "csv": "📊",
            "jpg": "🖼️", "jpeg": "🖼️", "png": "🖼️", "gif": "🖼️",
            "mp3": "🎵", "mp4": "🎬", "avi": "🎬",
            "zip": "📦", "rar": "📦", "7z": "📦",
            "exe": "⚙️", "dll": "⚙️",
            "js": "✨", "ts": "✨", "css": "🎨", "html": "🌐",
        };
        return iconMap[ext] || "📄";
    }

    formatFileSize(bytes) {
        if (bytes === 0) return "0 B";

        const units = ["B", "KB", "MB", "GB", "TB"];
        let size = bytes;
        let unitIndex = 0;

        while (size >= 1024 && unitIndex < units.length - 1) {
            size /= 1024;
            unitIndex++;
        }

        return `${size.toFixed(2)} ${units[unitIndex]}`;
    }

    showLoading(show) {
        if (show) {
            this.loadingIndicator.classList.remove("hidden");
        } else {
            this.loadingIndicator.classList.add("hidden");
        }
    }

    showError(message) {
        this.errorMessage.textContent = message;
        this.errorMessage.classList.remove("hidden");
    }

    hideError() {
        this.errorMessage.classList.add("hidden");
    }
}

// Initialize on page load
document.addEventListener("DOMContentLoaded", () => {
    console.log("DOM loaded, initializing FileBrowser");
    new FileBrowser();
});
