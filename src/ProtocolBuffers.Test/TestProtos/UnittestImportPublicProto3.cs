// Generated by the protocol buffer compiler.  DO NOT EDIT!
// source: google/protobuf/unittest_import_public_proto3.proto
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbd = global::Google.Protobuf.Descriptors;
using scg = global::System.Collections.Generic;
namespace Google.Protobuf.TestProtos {

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
  public static partial class UnittestImportPublicProto3 {

    #region Static variables
    internal static pbd::MessageDescriptor internal__static_protobuf_unittest_import_PublicImportMessage__Descriptor;
    internal static pb::FieldAccess.FieldAccessorTable<global::Google.Protobuf.TestProtos.PublicImportMessage> internal__static_protobuf_unittest_import_PublicImportMessage__FieldAccessorTable;
    #endregion
    #region Descriptor
    public static pbd::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbd::FileDescriptor descriptor;

    static UnittestImportPublicProto3() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "CjNnb29nbGUvcHJvdG9idWYvdW5pdHRlc3RfaW1wb3J0X3B1YmxpY19wcm90", 
            "bzMucHJvdG8SGHByb3RvYnVmX3VuaXR0ZXN0X2ltcG9ydCIgChNQdWJsaWNJ", 
            "bXBvcnRNZXNzYWdlEgkKAWUYASABKAVCNwoYY29tLmdvb2dsZS5wcm90b2J1", 
          "Zi50ZXN0qgIaR29vZ2xlLlByb3RvYnVmLlRlc3RQcm90b3NiBnByb3RvMw=="));
      pbd::FileDescriptor.InternalDescriptorAssigner assigner = delegate(pbd::FileDescriptor root) {
        descriptor = root;
        internal__static_protobuf_unittest_import_PublicImportMessage__Descriptor = Descriptor.MessageTypes[0];
        internal__static_protobuf_unittest_import_PublicImportMessage__FieldAccessorTable = 
            new pb::FieldAccess.FieldAccessorTable<global::Google.Protobuf.TestProtos.PublicImportMessage>(internal__static_protobuf_unittest_import_PublicImportMessage__Descriptor,
                new string[] { "E", });
      };
      pbd::FileDescriptor.InternalBuildGeneratedFileFrom(descriptorData,
          new pbd::FileDescriptor[] {
          }, assigner);
    }
    #endregion

  }
  #region Messages
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
  public sealed partial class PublicImportMessage : pb::IMessage<PublicImportMessage>, global::System.IEquatable<PublicImportMessage> {
    private static readonly pb::MessageParser<PublicImportMessage> _parser = new pb::MessageParser<PublicImportMessage>(() => new PublicImportMessage());
    public static pb::MessageParser<PublicImportMessage> Parser { get { return _parser; } }

    private static readonly string[] _fieldNames = new string[] { "e" };
    private static readonly uint[] _fieldTags = new uint[] { 8 };
    public static pbd::MessageDescriptor Descriptor {
      get { return global::Google.Protobuf.TestProtos.UnittestImportPublicProto3.internal__static_protobuf_unittest_import_PublicImportMessage__Descriptor; }
    }

    public pb::FieldAccess.FieldAccessorTable<PublicImportMessage> Fields {
      get { return global::Google.Protobuf.TestProtos.UnittestImportPublicProto3.internal__static_protobuf_unittest_import_PublicImportMessage__FieldAccessorTable; }
    }

    public PublicImportMessage() { }
    public PublicImportMessage(PublicImportMessage other) {
      MergeFrom(other);
    }
    public const int EFieldNumber = 1;
    private int e_;
    public int E {
      get { return e_; }
      set { e_ = value; }
    }


    public override bool Equals(object other) {
      return Equals(other as PublicImportMessage);
    }

    public bool Equals(PublicImportMessage other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (E != other.E) return false;
      return true;
    }

    public override int GetHashCode() {
      int hash = 0;
      if (E != 0) hash ^= E.GetHashCode();
      return hash;
    }

    public void WriteTo(pb::CodedOutputStream output) {
      if (E != 0) {
        output.WriteInt32(1, E);
      }
    }

    public int CalculateSize() {
      int size = 0;
      if (E != 0) {
        size += pb::CodedOutputStream.ComputeInt32Size(1, E);
      }
      return size;
    }
    public void MergeFrom(PublicImportMessage other) {
      if (other == null) {
        return;
      }
      if (other.E != 0) {
        E = other.E;
      }
    }

    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while (input.ReadTag(out tag)) {
        switch(tag) {
          case 0:
            throw pb::InvalidProtocolBufferException.InvalidTag();
          default:
            if (pb::WireFormat.IsEndGroupTag(tag)) {
              return;
            }
            break;
          case 8: {
            e_ = input.ReadInt32();
            break;
          }
        }
      }
    }

  }

  #endregion

}

#endregion Designer generated code
