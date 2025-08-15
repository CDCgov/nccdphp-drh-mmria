
/**
 * Implements array of functionality (constructors, methods properties for Form designer local revisions)
 */
; (function () {

    'use strict';

    function removeFromTo(array, from, to) {
        array.splice(from,
            !to ||
            1 + to - from + (!(to < 0 ^ from >= 0) && (to < 0 || -1) * array.length));
        return array.length;
    }

    var UndoManager = function () {

        var commands = [],
            index = -1,
            limit = 0,
            isExecuting = false,
            callback,

            // functions
            execute;

        execute = function (command, action) {
            if (!command || typeof command[action] !== "function") {
                return this;
            }
            isExecuting = true;

            command[action]();

            isExecuting = false;
            return this;
        };

        return {

            /*
            Add a command to the queue.
            */
            add: function (command) {
                if (isExecuting) {
                    return this;
                }
                // if we are here after having called undo,
                // invalidate items higher on the stack
                commands.splice(index + 1, commands.length - index);

                commands.push(command);

                // if limit is set, remove items from the start
                if (limit && commands.length > limit) {
                    removeFromTo(commands, 0, -(limit + 1));
                }

                // set the current index to the end
                index = commands.length - 1;
                if (callback) {
                    callback();
                }
                return this;
            },

            /*
            Pass a function to be called on undo and redo actions.
            */
            setCallback: function (callbackFunc) {
                callback = callbackFunc;
            },

            /*
            Perform undo: call the undo function at the current index and decrease the index by 1.
            */
            undo: function () {
                var command = commands[index];
                if (!command) {
                    return this;
                }
                execute(command, "undo");
                index -= 1;
                if (callback) {
                    callback();
                }
                return this;
            },

            /*
            Perform redo: call the redo function at the next index and increase the index by 1.
            */
            redo: function () {
                var command = commands[index + 1];
                if (!command) {
                    return this;
                }
                execute(command, "redo");
                index += 1;
                if (callback) {
                    callback();
                }
                return this;
            },

            /*
            Clears the memory, losing all stored states. Reset the index.
            */
            clear: function () {
                var prev_size = commands.length;

                commands = [];
                index = -1;

                if (callback && (prev_size > 0)) {
                    callback();
                }
            },

            hasUndo: function () {
                return index !== -1;
            },

            hasRedo: function () {
                return index < (commands.length - 1);
            },

            getCommands: function () {
                return commands;
            },

            getIndex: function () {
                return index;
            },

            setLimit: function (l) {
                limit = l;
            }
        };
    };

    if (typeof define === 'function' && typeof define.amd === 'object' && define.amd) {
        // AMD. Register as an anonymous module.
        define(function () {
            return UndoManager;
        });
    } else if (typeof module !== 'undefined' && module.exports) {
        module.exports = UndoManager;
    } else {
        window.UndoManager = UndoManager;
    }

}());

/*** Start Scracth Work - To delete later 
 * *************************************************************************************************
function handleLocalFDRevisons() {
	localFDRevCountCurrent = localFDRevCount;
	localFDRev[localFDRevCountCurrent] = JSON.parse(JSON.stringify(uiSpecification));
	console.log('current count', localFDRevCountCurrent);
	console.log(localFDRev);
	localFDRevCount++;
	console.log('next count', localFDRevCount);
}

function revertFDEvents() {
	if (localFDRevCountCurrent < 1) {
		alert('You have gone back to last saved revision. Can\'t undo anymore');
		localFDRevCount = 2;
		localFDRevCountCurrent = 1;
		console.log('after revert', localFDRevCount);
	}
	let revertCount = localFDRevCountCurrent - 1;
	uiSpecification = localFDRev[revertCount];
	localFDRevCount--;
	localFDRevCountCurrent--;
	console.log("current count", localFDRevCountCurrent);
	$.get(urlMetaData, function (data, status) {
		var metaDataForms = buildFormList(data);

		var caseForm = metaDataForms.find(x => x.name === activeForm);

		// Set what type of fields you would like
		formElements = groupFormElementsByType(caseForm);

		buildFormElementPromptControl(formElements);
		console.log(localFDRevCount);

	});
}
 * *************************************************************************************************
 */