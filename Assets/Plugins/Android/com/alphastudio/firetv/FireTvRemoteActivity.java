package com.alphastudio.firetv;

import android.view.KeyEvent;
import com.unity3d.player.UnityPlayerActivity;

/**
 * Fire TV Stick remote. Unity never reports Play/Pause, so this activity records
 * that key, plus Back, Menu, and the center Select button, for the game to read.
 */
public class FireTvRemoteActivity extends UnityPlayerActivity {
    private static boolean playPausePending;
    private static boolean selectPending;
    private static boolean selectUpPending;
    private static boolean backPending;
    private static boolean menuPending;
    private static boolean selectHeld;

    public static boolean consumePlayPause() {
        if (!playPausePending)
            return false;
        playPausePending = false;
        return true;
    }

    public static boolean consumeSelect() {
        if (!selectPending)
            return false;
        selectPending = false;
        return true;
    }

    public static boolean consumeSelectUp() {
        if (!selectUpPending)
            return false;
        selectUpPending = false;
        return true;
    }

    public static boolean consumeBack() {
        if (!backPending)
            return false;
        backPending = false;
        return true;
    }

    public static boolean consumeMenu() {
        if (!menuPending)
            return false;
        menuPending = false;
        return true;
    }

    public static boolean isSelectHeld() {
        return selectHeld;
    }

    @Override
    public boolean dispatchKeyEvent(KeyEvent event) {
        note(event);
        boolean handled = super.dispatchKeyEvent(event);
        if (isPlayPause(event.getKeyCode()))
            return true;
        return handled;
    }

    @Override
    public boolean onKeyDown(int keyCode, KeyEvent event) {
        note(event);
        boolean handled = super.onKeyDown(keyCode, event);
        if (isPlayPause(keyCode))
            return true;
        return handled;
    }

    @Override
    public boolean onKeyUp(int keyCode, KeyEvent event) {
        note(event);
        boolean handled = super.onKeyUp(keyCode, event);
        if (isPlayPause(keyCode))
            return true;
        return handled;
    }

    private static void note(KeyEvent event) {
        if (event == null)
            return;

        int key = event.getKeyCode();
        int action = event.getAction();
        if (action == KeyEvent.ACTION_DOWN) {
            if (isSelect(key))
                selectHeld = true;
            if (event.getRepeatCount() == 0)
                markEdge(key);
            return;
        }

        if (action == KeyEvent.ACTION_UP && isSelect(key)) {
            selectHeld = false;
            selectUpPending = true;
        }
    }

    private static void markEdge(int key) {
        if (isPlayPause(key))
            playPausePending = true;
        if (isSelect(key))
            selectPending = true;
        if (key == KeyEvent.KEYCODE_BACK)
            backPending = true;
        if (key == KeyEvent.KEYCODE_MENU)
            menuPending = true;
    }

    private static boolean isPlayPause(int key) {
        return key == KeyEvent.KEYCODE_MEDIA_PLAY_PAUSE
            || key == KeyEvent.KEYCODE_MEDIA_PLAY
            || key == KeyEvent.KEYCODE_MEDIA_PAUSE;
    }

    private static boolean isSelect(int key) {
        return key == KeyEvent.KEYCODE_DPAD_CENTER
            || key == KeyEvent.KEYCODE_BUTTON_A
            || key == KeyEvent.KEYCODE_ENTER
            || key == KeyEvent.KEYCODE_NUMPAD_ENTER;
    }
}
