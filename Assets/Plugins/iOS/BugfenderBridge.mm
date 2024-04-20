#import <Foundation/Foundation.h>
#import "Libraries/Plugins/iOS/BugfenderSDK.h"

extern "C" {
  void BugfenderActivateLogger(const char* key) {
    [Bugfender activateLogger:[NSString stringWithUTF8String:key]];
  }
  
  void BugfenderEnableCrashReporting() {
    [Bugfender enableCrashReporting];
  }
  
  void BugfenderSetDeviceString(const char* key, const char* value) {
    [Bugfender setDeviceString:[NSString stringWithUTF8String:value] forKey:[NSString stringWithUTF8String:key]];
  }
  
  void BugfenderRemoveDeviceKey(const char* key) {
    [Bugfender removeDeviceKey:[NSString stringWithUTF8String:key]];
  }
  
  void BugfenderLog(int logLevel, const char* tag, const char* message) {
    [Bugfender logWithLineNumber:0 method:@"" file:@"" level:(BFLogLevel)logLevel tag:[NSString stringWithUTF8String:tag] message:[NSString stringWithUTF8String:message]];
  }
  
  void BugfenderSendIssue(const char* title, const char* markdown) {
    [Bugfender sendIssueWithTitle:[NSString stringWithUTF8String:title] text:[NSString stringWithUTF8String:markdown]];
  }
}