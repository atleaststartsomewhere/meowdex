window.meowdex = window.meowdex || {};

window.meowdex.scrollToCanvas = (containerId, targetId) => {
    const container = document.getElementById(containerId);
    const target = document.getElementById(targetId);
    if (!container || !target) {
        return;
    }

    const left = target.offsetLeft - container.offsetLeft;
    container.scrollTo({ left, behavior: "smooth" });
};
