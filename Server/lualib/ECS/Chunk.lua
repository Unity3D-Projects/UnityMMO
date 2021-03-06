local Chunk = BaseClass()
ECS.Chunk = Chunk
ECS.Chunk.kChunkSize = 16 * 1024

function Chunk:Constructor(  )
	self.Buffer = ECSCore.CreateChunk(ECS.Chunk.kChunkSize)
	self.Count = 0--当前Entity的数量
	self.Capacity = 0--能存放Entity的容量
	self.SharedComponentValueArray = {}
	self.Archetype = nil
	self.ChunkListNode = nil
	self.ChunkListWithEmptySlotsNode = nil
	
end

function Chunk.GetChunkBufferSize( numComponents, numSharedComponents )
	-- local bufferSize = ECS.Chunk.kChunkSize - (numSharedComponents * sizeof(int) + numComponents * sizeof(uint))
	local bufferSize = ECS.Chunk.kChunkSize - (numSharedComponents * 4 + numComponents * 4)
    return bufferSize
end

function Chunk.GetSharedComponentOffset( numSharedComponents )
    return ECS.Chunk.kChunkSize - numSharedComponents * ECS.CoreHelper.GetIntegerSize()
end

function Chunk.GetChangedComponentOffset( numComponents, numSharedComponents )
    return Chunk.GetSharedComponentOffset(numSharedComponents) - numComponents * ECS.CoreHelper.GetIntegerSize()
end

return Chunk