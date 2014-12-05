#import <GameKit/GameKit.h>

extern "C"
{
    void UnitySendMessage(const char* name, const char* fn, const char* data);
    
    bool gameCenterAreChallengesAvailable()
    {
        return NSClassFromString(@"GKChallenge");
    }
    
    // for simplicity, as a first implementation just load the achievements again instead of caching it.
    void _IssueChallengeAchievement(const char* achievementName, const char* players, const char* message)
    {
        if(gameCenterAreChallengesAvailable())
        {
            NSLog(@"->IssueChallengeAchievement for: %s", achievementName);
            NSLog(@"       ->players: %s", players);
            NSLog(@"   ->message: %s", message);
            NSString* messageString = [NSString stringWithUTF8String:message];
            NSString* achievementString = [NSString stringWithUTF8String:achievementName];
            NSString* playersString = [NSString stringWithUTF8String:players];
            NSArray* challengePlayerIDs = [playersString componentsSeparatedByString:@";"];
            
            [GKAchievement loadAchievementsWithCompletionHandler:^(NSArray * achievements, NSError *error)
             {
                 if(achievements)
                 {
                     for(GKAchievement* achievement in achievements)
                     {
                         if([achievement.identifier isEqualToString:achievementString])
                         {
                             NSLog(@"Found achievement!");
                             [achievement issueChallengeToPlayers:challengePlayerIDs message:messageString];
                         }
                     }
                 }
             }];
        }
        else
        {
            NSLog(@"Game center challenges not available");
        }
    }
    
    // this requires an async operation, so cannot return directly
    void _GetAchievementChallengeablePlayers(const char* achievementName)
    {
        if(gameCenterAreChallengesAvailable())
        {
            NSLog(@"->_GetAchievementChallengeablePlayers for :%s", achievementName);
            
            NSString* achievementString = [NSString stringWithUTF8String:achievementName];
            
            [GKAchievement loadAchievementsWithCompletionHandler:^(NSArray * achievements, NSError *error)
             {
                 if(achievements)
                 {
                     for(GKAchievement* achievement in achievements)
                     {
                         if([achievement.identifier isEqualToString:achievementString])
                         {
                             NSLog(@"Found achievement!");
                             [achievement selectChallengeablePlayerIDs:[GKLocalPlayer localPlayer].friends withCompletionHandler:^(NSArray *challengeablePlayerIDs, NSError *error) {
                                 if (challengeablePlayerIDs)
                                 {
                                     NSLog(@"Found Challengeable players!");
                                     NSString* playerString = @"";
                                     for(NSString* playerID in challengeablePlayerIDs)
                                     {
                                         NSLog(@" ---> %@", playerID);
                                         playerString = [playerString stringByAppendingString:playerID];
                                         playerString = [playerString stringByAppendingString:@";"];
                                     }
                                     
                                     NSLog(@"Concatenated player string: %@", playerString);
                                     
                                     UnitySendMessage("gc_callbacks", "OnAchievementChallengePlayerList", [playerString UTF8String]);
                                     
                                 }
                             }];
                             return;
                         }
                         else
                         {
                             NSLog(@"-> %@",achievement.identifier);
                         }
                     }
                 }
                 
             }];
        }
        else
        {
            NSLog(@"Game center challenges not available");
        }
        
    }
    
    void _GetScoreChallengeablePlayers(const char* leaderboardId, int playerScore )
    {
        if(gameCenterAreChallengesAvailable())
        {
            NSLog(@"->GetScoreChallengeablePlayers for: %s", leaderboardId);
            
            NSString* leaderboardCategory = [NSString stringWithUTF8String:leaderboardId];
            
            GKLeaderboard *query = [[GKLeaderboard alloc] init];
            query.category = leaderboardCategory;
            query.playerScope = GKLeaderboardPlayerScopeFriendsOnly;
            // might need this...
            //query.range = NSMakeRange(1,100);
            
            [query loadScoresWithCompletionHandler:^(NSArray* scores, NSError* error)
             {
                 if(scores)
                 {
                     NSLog(@"Retrieved scores!");
                     NSLog(@"local player id is %@", query.localPlayerScore.playerID);
                     NSString* playerString = @"";
                     
                     NSString* predicateString = [NSString stringWithFormat:@"value > %d", playerScore];
                     NSPredicate* filter = [NSPredicate predicateWithFormat:predicateString];
                     NSArray* lesserScores = [scores filteredArrayUsingPredicate:filter];
                     
                     for(GKScore* score in lesserScores)
                     {
                         NSLog(@"--> %@", score.playerID);
                         if(![score.playerID isEqualToString:query.localPlayerScore.playerID])
                         {
                             playerString = [playerString stringByAppendingString:score.playerID];
                             playerString = [playerString stringByAppendingString:@";"];
                         }
                     }
                     
                     NSLog(@"Concatenated player string: %@", playerString);
                     UnitySendMessage("gc_callbacks", "OnScoreChallengePlayerList", [playerString UTF8String]);
                 }
                 //            NSPredicate* filter = [NSPredicate predicateWithFormat:@"value < %qi",score];
                 //            NSArray* lesserScores = [scores filteredarrayUsingPredicate:filter];
                 
             }];
            
        }
        else
        {
            NSLog(@"Game center challenges not available");
        }
    }
    
    // for simplicity, as a first implementation just load the score again instead of caching it.
    void _IssueChallengeScore(const char* leaderboardId, const char* players, const char* message)
    {
        if(gameCenterAreChallengesAvailable())
        {
            NSLog(@"->IssueChallengeScore for: %s", leaderboardId);
            NSLog(@"       ->players: %s", players);
            NSLog(@"   ->message: %s", message);
            NSString* messageString = [NSString stringWithUTF8String:message];
            NSString* leaderboardString = [NSString stringWithUTF8String:leaderboardId];
            NSString* playersString = [NSString stringWithUTF8String:players];
            NSArray* challengePlayerIDs = [playersString componentsSeparatedByString:@";"];
            
            GKLeaderboard *query = [[GKLeaderboard alloc] init];
            query.category = leaderboardString;
            query.playerScope = GKLeaderboardPlayerScopeFriendsOnly;
            query.range = NSMakeRange(1,1);
            
            // query.localPlayerScores is invalid until a call to loadScoresWithCompletionHandler. I don't actually need the scores...
            [query loadScoresWithCompletionHandler:^(NSArray* scores, NSError* error)
             {
                 if(scores)
                 {
                     NSLog(@"Retrieved scores!");
                     GKScore* localPlayerScore = query.localPlayerScore;
                     if(localPlayerScore == nil)
                     {
                         NSLog(@"score is nil!!!");
                     }
                     else
                     {
                         NSLog(@"score is not nil, issuing challenge!");
                         [localPlayerScore issueChallengeToPlayers:challengePlayerIDs message:messageString];
                     }
                 }
                 
             }];
            
        }
        else
        {
            NSLog(@"Game center challenges not available");
        }
        
        
    }
    
    
    void _GetLeaderboardTitle(const char* leaderboardId)
    {
        NSString* leaderboardString = [NSString stringWithUTF8String:leaderboardId];
        GKLeaderboard *query = [[GKLeaderboard alloc] init];
        query.category = leaderboardString;
        query.playerScope = GKLeaderboardPlayerScopeFriendsOnly;
        query.range = NSMakeRange(1,1);
        
        // query.title is invalid until a call to loadScoresWithCompletionHandler. I don't actually need the scores...
        [query loadScoresWithCompletionHandler:^(NSArray* scores, NSError* error)
         {
             if(scores)
             {
                 NSString* lbData = [leaderboardString stringByAppendingString:@";"];
                 lbData = [lbData stringByAppendingString:query.title];
                 
                 
                 UnitySendMessage("gc_callbacks", "OnLeaderboardTitle", [lbData UTF8String]);
             }
         }];
    }
    
    void _GetActiveChallenges()
    {
        if(gameCenterAreChallengesAvailable())
        {
            [GKChallenge loadReceivedChallengesWithCompletionHandler:^(NSArray *challenges, NSError *error) {
                if (challenges)
                {
                    NSString* challengeString = @"";
                    for(GKChallenge* challenge in challenges)
                    {
                        NSLog(@"challenge from %@", challenge.issuingPlayerID);
                        NSTimeInterval interval = challenge.issueDate.timeIntervalSinceReferenceDate;
                        NSLog(@"  issued on %f", interval);
                        challengeString = [challengeString stringByAppendingString:challenge.issuingPlayerID];
                        challengeString = [challengeString stringByAppendingString:@"|"];
                        challengeString = [challengeString stringByAppendingFormat:@"%f",interval];
                        challengeString = [challengeString stringByAppendingString:@"|"];
                        
                        if([challenge isKindOfClass:[GKAchievementChallenge class]])
                        {
                            GKAchievementChallenge* aChallenge = (GKAchievementChallenge*)challenge;
                            GKAchievement* achievement = aChallenge.achievement;
                            NSLog(@"  for achievement %@", achievement.identifier);
                            
                            challengeString = [challengeString stringByAppendingString:@"achievement"];
                            challengeString = [challengeString stringByAppendingString:@"|"];
                            challengeString = [challengeString stringByAppendingString:achievement.identifier];
                            challengeString = [challengeString stringByAppendingString:@"|"];
                            if(aChallenge.message != nil)
                            {
                                challengeString = [challengeString stringByAppendingString:aChallenge.message];
                            }
                        }
                        else if([challenge isKindOfClass:[GKScoreChallenge class]])
                        {
                            GKScoreChallenge* sChallenge = (GKScoreChallenge*)challenge;
                            GKScore* score = sChallenge.score;
                            NSLog(@"   for leaderboard %@", score.category);
                            NSLog(@"   for score %lld", score.value);
                            
                            challengeString = [challengeString stringByAppendingString:@"score"];
                            challengeString = [challengeString stringByAppendingString:@"|"];
                            challengeString = [challengeString stringByAppendingString:score.category];
                            challengeString = [challengeString stringByAppendingString:@"|"];
                            challengeString = [challengeString stringByAppendingFormat:@"%lld",score.value];
                            challengeString = [challengeString stringByAppendingString:@"|"];
                            if(sChallenge.message != nil)
                            {
                                challengeString = [challengeString stringByAppendingString:sChallenge.message];
                            }
                        }
                        
                        challengeString = [challengeString stringByAppendingString:@";"];
                        
                    }
                    NSLog(@"Concatenated challenge string: %@", challengeString);
                    UnitySendMessage("gc_callbacks", "OnActiveChallengeList", [challengeString UTF8String]);
                    
                }
                else
                {
                    NSLog(@"No challenges!");
                }
            }];
        }
        else
        {
            NSLog(@"Game center challenges not available");
        }
        
    }
    
    // challengeId is the achievement.identifier or the score.category
    void _DeclineChallenge(const char* challengerId, const char* challengeId)
    {
        if(gameCenterAreChallengesAvailable())
        {
            NSString* challengerString = [NSString stringWithUTF8String:challengerId];
            NSString* challengeIdString = [NSString stringWithUTF8String:challengeId];

            [GKChallenge loadReceivedChallengesWithCompletionHandler:^(NSArray *challenges, NSError *error) {
                if (challenges)
                {
                    
                    for(GKChallenge* challenge in challenges)
                    {
                        if([challenge.issuingPlayerID isEqualToString:challengerString])
                        {
                            if([challenge isKindOfClass:[GKAchievementChallenge class]])
                            {
                                GKAchievementChallenge* aChallenge = (GKAchievementChallenge*)challenge;
                                GKAchievement* achievement = aChallenge.achievement;
                                if([achievement.identifier isEqualToString:challengeIdString] )
                                {
                                    [aChallenge decline];
                                    return;
                                }
                            }
                            else if([challenge isKindOfClass:[GKScoreChallenge class]])
                            {
                                GKScoreChallenge* sChallenge = (GKScoreChallenge*)challenge;
                                GKScore* score = sChallenge.score;
                                
                                if([score.category isEqualToString:challengeIdString])
                                {
                                    [sChallenge decline];
                                    return;
                                }
                            }
                        }
                    }
                    
                }
                else
                {
                    NSLog(@"No challenges!");
                }
            }];
        }
        else
        {
            NSLog(@"Game center challenges not available");
        }
    }
}
