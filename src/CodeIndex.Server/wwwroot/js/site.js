function ScorllTextAreaToBottom(itemID) {
    var textarea = document.getElementById(itemID);
    textarea.scrollTop = textarea.scrollHeight;
}