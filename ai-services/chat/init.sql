-- BrainStormEra Chat Service Database Initialization

-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Create accounts table (if not exists from main application)
CREATE TABLE IF NOT EXISTS accounts (
    user_id VARCHAR PRIMARY KEY DEFAULT uuid_generate_v4()::text,
    username VARCHAR(100) NOT NULL,
    user_email VARCHAR(255) NOT NULL UNIQUE,
    full_name VARCHAR(255),
    user_image VARCHAR(500),
    last_active TIMESTAMP,
    is_online BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create conversations table
CREATE TABLE IF NOT EXISTS conversations (
    conversation_id VARCHAR PRIMARY KEY DEFAULT uuid_generate_v4()::text,
    participant1_id VARCHAR NOT NULL REFERENCES accounts(user_id) ON DELETE CASCADE,
    participant2_id VARCHAR NOT NULL REFERENCES accounts(user_id) ON DELETE CASCADE,
    last_message_id VARCHAR,
    last_message_at TIMESTAMP,
    conversation_created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    conversation_updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(participant1_id, participant2_id)
);

-- Create messages table
CREATE TABLE IF NOT EXISTS messages (
    message_id VARCHAR PRIMARY KEY DEFAULT uuid_generate_v4()::text,
    sender_id VARCHAR NOT NULL REFERENCES accounts(user_id) ON DELETE CASCADE,
    receiver_id VARCHAR NOT NULL REFERENCES accounts(user_id) ON DELETE CASCADE,
    conversation_id VARCHAR NOT NULL REFERENCES conversations(conversation_id) ON DELETE CASCADE,
    message_content TEXT NOT NULL,
    message_type VARCHAR(20) DEFAULT 'text' CHECK (message_type IN ('text', 'image', 'file', 'system')),
    is_read BOOLEAN DEFAULT FALSE,
    is_deleted_by_sender BOOLEAN DEFAULT FALSE,
    is_deleted_by_receiver BOOLEAN DEFAULT FALSE,
    reply_to_message_id VARCHAR,
    is_edited BOOLEAN DEFAULT FALSE,
    message_created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    message_updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create chatbot_conversations table
CREATE TABLE IF NOT EXISTS chatbot_conversations (
    conversation_id VARCHAR PRIMARY KEY DEFAULT uuid_generate_v4()::text,
    user_id VARCHAR NOT NULL REFERENCES accounts(user_id) ON DELETE CASCADE,
    conversation_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    user_message TEXT NOT NULL,
    bot_response TEXT NOT NULL,
    conversation_context TEXT,
    feedback_rating INTEGER CHECK (feedback_rating >= 1 AND feedback_rating <= 5),
    feedback_comment TEXT,
    feedback_submitted_at TIMESTAMP
);

-- Create chatbot_cache table
CREATE TABLE IF NOT EXISTS chatbot_cache (
    cache_key VARCHAR PRIMARY KEY,
    cached_response TEXT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP NOT NULL
);

-- Create user_sessions table
CREATE TABLE IF NOT EXISTS user_sessions (
    session_id VARCHAR PRIMARY KEY DEFAULT uuid_generate_v4()::text,
    user_id VARCHAR NOT NULL REFERENCES accounts(user_id) ON DELETE CASCADE,
    connection_id VARCHAR,
    connected_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    disconnected_at TIMESTAMP,
    is_active BOOLEAN DEFAULT TRUE
);

-- Create conversation_analytics table
CREATE TABLE IF NOT EXISTS conversation_analytics (
    id VARCHAR PRIMARY KEY DEFAULT uuid_generate_v4()::text,
    date TIMESTAMP NOT NULL,
    total_conversations INTEGER DEFAULT 0,
    unique_users INTEGER DEFAULT 0,
    average_response_time FLOAT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for better performance

-- Conversations indexes
CREATE INDEX IF NOT EXISTS idx_conversations_participant1 ON conversations(participant1_id);
CREATE INDEX IF NOT EXISTS idx_conversations_participant2 ON conversations(participant2_id);
CREATE INDEX IF NOT EXISTS idx_conversations_last_message_at ON conversations(last_message_at DESC);

-- Messages indexes
CREATE INDEX IF NOT EXISTS idx_messages_sender_receiver ON messages(sender_id, receiver_id);
CREATE INDEX IF NOT EXISTS idx_messages_conversation ON messages(conversation_id);
CREATE INDEX IF NOT EXISTS idx_messages_created_at ON messages(message_created_at DESC);
CREATE INDEX IF NOT EXISTS idx_messages_unread ON messages(receiver_id, is_read) WHERE NOT is_read;
CREATE INDEX IF NOT EXISTS idx_messages_reply_to ON messages(reply_to_message_id) WHERE reply_to_message_id IS NOT NULL;

-- Chatbot conversations indexes
CREATE INDEX IF NOT EXISTS idx_chatbot_conversations_user ON chatbot_conversations(user_id);
CREATE INDEX IF NOT EXISTS idx_chatbot_conversations_time ON chatbot_conversations(conversation_time DESC);
CREATE INDEX IF NOT EXISTS idx_chatbot_conversations_rating ON chatbot_conversations(feedback_rating) WHERE feedback_rating IS NOT NULL;

-- Cache indexes
CREATE INDEX IF NOT EXISTS idx_chatbot_cache_expires ON chatbot_cache(expires_at);

-- User sessions indexes
CREATE INDEX IF NOT EXISTS idx_user_sessions_user ON user_sessions(user_id);
CREATE INDEX IF NOT EXISTS idx_user_sessions_active ON user_sessions(is_active) WHERE is_active;

-- Analytics indexes
CREATE INDEX IF NOT EXISTS idx_conversation_analytics_date ON conversation_analytics(date DESC);

-- Create updated_at trigger function
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Create triggers for updated_at
CREATE TRIGGER update_accounts_updated_at BEFORE UPDATE ON accounts
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_conversations_updated_at BEFORE UPDATE ON conversations
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_messages_updated_at BEFORE UPDATE ON messages
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Insert sample data for testing (optional)
-- INSERT INTO accounts (user_id, username, user_email, full_name) VALUES
-- ('user1', 'alice', 'alice@example.com', 'Alice Johnson'),
-- ('user2', 'bob', 'bob@example.com', 'Bob Smith'),
-- ('user3', 'charlie', 'charlie@example.com', 'Charlie Brown');

-- Cleanup old cache entries (for maintenance)
CREATE OR REPLACE FUNCTION cleanup_expired_cache()
RETURNS INTEGER AS $$
DECLARE
    deleted_count INTEGER;
BEGIN
    DELETE FROM chatbot_cache WHERE expires_at < CURRENT_TIMESTAMP;
    GET DIAGNOSTICS deleted_count = ROW_COUNT;
    RETURN deleted_count;
END;
$$ LANGUAGE plpgsql;

-- Create a view for conversation summaries
CREATE OR REPLACE VIEW conversation_summaries AS
SELECT 
    c.conversation_id,
    c.participant1_id,
    c.participant2_id,
    a1.username as participant1_name,
    a2.username as participant2_name,
    c.last_message_at,
    m.message_content as last_message,
    COUNT(m2.message_id) as total_messages,
    COUNT(CASE WHEN m2.is_read = FALSE THEN 1 END) as unread_messages
FROM conversations c
LEFT JOIN accounts a1 ON c.participant1_id = a1.user_id
LEFT JOIN accounts a2 ON c.participant2_id = a2.user_id
LEFT JOIN messages m ON c.last_message_id = m.message_id
LEFT JOIN messages m2 ON c.conversation_id = m2.conversation_id
GROUP BY c.conversation_id, a1.username, a2.username, c.last_message_at, m.message_content;

-- Create a view for chatbot analytics
CREATE OR REPLACE VIEW chatbot_analytics AS
SELECT 
    DATE(conversation_time) as conversation_date,
    COUNT(*) as daily_conversations,
    COUNT(DISTINCT user_id) as unique_users,
    AVG(feedback_rating) as average_rating,
    COUNT(CASE WHEN feedback_rating IS NOT NULL THEN 1 END) as rated_conversations,
    COUNT(CASE WHEN feedback_rating >= 4 THEN 1 END) as positive_ratings
FROM chatbot_conversations
GROUP BY DATE(conversation_time)
ORDER BY conversation_date DESC; 