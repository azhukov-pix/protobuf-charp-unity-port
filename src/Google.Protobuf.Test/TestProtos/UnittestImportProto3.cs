// Generated by the protocol buffer compiler.  DO NOT EDIT!
// source: google/protobuf/unittest_import_proto3.proto
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace Google.Protobuf.TestProtos {

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
  /// <summary>Holder for reflection information generated from google/protobuf/unittest_import_proto3.proto</summary>
  public static partial class UnittestImportProto3 {

    #region Descriptor
    /// <summary>File descriptor for google/protobuf/unittest_import_proto3.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static UnittestImportProto3() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "Cixnb29nbGUvcHJvdG9idWYvdW5pdHRlc3RfaW1wb3J0X3Byb3RvMy5wcm90",
            "bxIYcHJvdG9idWZfdW5pdHRlc3RfaW1wb3J0GjNnb29nbGUvcHJvdG9idWYv",
            "dW5pdHRlc3RfaW1wb3J0X3B1YmxpY19wcm90bzMucHJvdG8iGgoNSW1wb3J0",
            "TWVzc2FnZRIJCgFkGAEgASgFKlkKCkltcG9ydEVudW0SGwoXSU1QT1JUX0VO",
            "VU1fVU5TUEVDSUZJRUQQABIOCgpJTVBPUlRfRk9PEAcSDgoKSU1QT1JUX0JB",
            "UhAIEg4KCklNUE9SVF9CQVoQCUI8Chhjb20uZ29vZ2xlLnByb3RvYnVmLnRl",
            "c3RIAfgBAaoCGkdvb2dsZS5Qcm90b2J1Zi5UZXN0UHJvdG9zUABiBnByb3Rv",
            "Mw=="));
      descriptor = pbr::FileDescriptor.InternalBuildGeneratedFileFrom(descriptorData,
          new pbr::FileDescriptor[] { global::Google.Protobuf.TestProtos.UnittestImportPublicProto3.Descriptor, },
          new pbr::GeneratedCodeInfo(new[] {typeof(global::Google.Protobuf.TestProtos.ImportEnum), }, new pbr::GeneratedCodeInfo[] {
            new pbr::GeneratedCodeInfo(typeof(global::Google.Protobuf.TestProtos.ImportMessage), new[]{ "D" }, null, null, null)
          }));
    }
    #endregion

  }
  #region Enums
  public enum ImportEnum {
    IMPORT_ENUM_UNSPECIFIED = 0,
    IMPORT_FOO = 7,
    IMPORT_BAR = 8,
    IMPORT_BAZ = 9,
  }

  #endregion

  #region Messages
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
  public sealed partial class ImportMessage : pb::IMessage<ImportMessage> {
    private static readonly pb::MessageParser<ImportMessage> _parser = new pb::MessageParser<ImportMessage>(() => new ImportMessage());
    public static pb::MessageParser<ImportMessage> Parser { get { return _parser; } }

    public static pbr::MessageDescriptor Descriptor {
      get { return global::Google.Protobuf.TestProtos.UnittestImportProto3.Descriptor.MessageTypes[0]; }
    }

    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    public ImportMessage() {
      OnConstruction();
    }

    partial void OnConstruction();

    public ImportMessage(ImportMessage other) : this() {
      d_ = other.d_;
    }

    public ImportMessage Clone() {
      return new ImportMessage(this);
    }

    public const int DFieldNumber = 1;
    private int d_;
    public int D {
      get { return d_; }
      set {
        d_ = value;
      }
    }

    public override bool Equals(object other) {
      return Equals(other as ImportMessage);
    }

    public bool Equals(ImportMessage other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (D != other.D) return false;
      return true;
    }

    public override int GetHashCode() {
      int hash = 1;
      if (D != 0) hash ^= D.GetHashCode();
      return hash;
    }

    public override string ToString() {
      return pb::JsonFormatter.Default.Format(this);
    }

    public void WriteTo(pb::CodedOutputStream output) {
      if (D != 0) {
        output.WriteRawTag(8);
        output.WriteInt32(D);
      }
    }

    public int CalculateSize() {
      int size = 0;
      if (D != 0) {
        size += 1 + pb::CodedOutputStream.ComputeInt32Size(D);
      }
      return size;
    }

    public void MergeFrom(ImportMessage other) {
      if (other == null) {
        return;
      }
      if (other.D != 0) {
        D = other.D;
      }
    }

    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            input.SkipLastField();
            break;
          case 8: {
            D = input.ReadInt32();
            break;
          }
        }
      }
    }

  }

  #endregion

}

#endregion Designer generated code
