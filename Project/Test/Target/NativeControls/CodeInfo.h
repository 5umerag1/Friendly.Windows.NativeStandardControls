#pragma once

/**
	@brief	�ʒm�R�[�h���B
*/
struct CodeInfo
{
	//�_�C�A���OID�B
	int dialogId;

	//���b�Z�[�W
	int msg;

	//�ʒm�R�[�h�B
	int code;

	//�X�N���[���o�[�R�[�h
	int nSBCode;
	
	//�X�N���[���ʒu
	int nPos;

	CodeInfo() : dialogId(), msg(), code(), nSBCode(), nPos() {}
	CodeInfo(int dialogId_, int msg_, int code_, int nSBCode_, int nPos_) : dialogId(dialogId_), msg(msg_), code(code_), nSBCode(nSBCode_), nPos(nPos_)  {}
};